Imports Windows.Devices.Bluetooth
Imports Windows.Devices.Bluetooth.GenericAttributeProfile

Public Class DeviceServicesForm
    Private currentDevice As BluetoothLEDevice
    Private servicesList As New List(Of GattDeviceService)
    Private characteristicsList As New List(Of GattCharacteristic)
    Private gattSession As GattSession

    Public Sub New(device As BluetoothLEDevice, gattServices As IReadOnlyList(Of GattDeviceService))
        InitializeComponent()
        currentDevice = device
        LoadDeviceInfo(gattServices)
        InitializeConnectionMonitoring()
    End Sub

    Private Async Sub InitializeConnectionMonitoring()
        Try
            ' Monitor connection status changes
            AddHandler currentDevice.ConnectionStatusChanged, AddressOf OnConnectionStatusChanged

            ' Create GATT session to maintain connection
            gattSession = Await GattSession.FromDeviceIdAsync(currentDevice.BluetoothDeviceId)
            If gattSession IsNot Nothing Then
                gattSession.MaintainConnection = True
            End If

            lblConnectionStatus.Text = $"Status: {currentDevice.ConnectionStatus} - Ready"
            lblConnectionStatus.ForeColor = Color.Green

        Catch ex As Exception
            lblConnectionStatus.Text = $"Status: {currentDevice.ConnectionStatus}"
            lblConnectionStatus.ForeColor = Color.Orange
        End Try
    End Sub

    Private Sub OnConnectionStatusChanged(sender As BluetoothLEDevice, args As Object)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() OnConnectionStatusChanged(sender, args))
                Return
            End If

            If sender.ConnectionStatus = BluetoothConnectionStatus.Disconnected Then
                lblConnectionStatus.Text = "Status: ⚠ DISCONNECTED"
                lblConnectionStatus.ForeColor = Color.Red
                MessageBox.Show("Device has been disconnected. The form will now close.", "Disconnected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Me.Close()
            Else
                lblConnectionStatus.Text = $"Status: {sender.ConnectionStatus}"
                lblConnectionStatus.ForeColor = Color.Green
            End If
        Catch ex As Exception
            ' Ignore errors in event handler
        End Try
    End Sub

    Private Sub LoadDeviceInfo(gattServices As IReadOnlyList(Of GattDeviceService))
        lblDeviceName.Text = If(String.IsNullOrEmpty(currentDevice.Name), "Unknown Device", currentDevice.Name)
        lblDeviceAddress.Text = $"Address: {currentDevice.BluetoothAddress:X12}"
        lblConnectionStatus.Text = $"Status: {currentDevice.ConnectionStatus}"

        lblServicesTitle.Text = $"Available Services ({gattServices.Count})"

        servicesList.Clear()
        lstServices.Items.Clear()

        For Each service In gattServices
            Dim serviceName As String = GetServiceName(service.Uuid)
            lstServices.Items.Add(serviceName)
            servicesList.Add(service)
        Next
    End Sub

    Private Function GetServiceName(uuid As Guid) As String
        Dim knownServices As New Dictionary(Of String, String) From {
            {"00001800-0000-1000-8000-00805f9b34fb", "Generic Access"},
            {"00001801-0000-1000-8000-00805f9b34fb", "Generic Attribute"},
            {"0000180a-0000-1000-8000-00805f9b34fb", "Device Information"},
            {"0000180f-0000-1000-8000-00805f9b34fb", "Battery Service"},
            {"00001805-0000-1000-8000-00805f9b34fb", "Current Time Service"},
            {"00001810-0000-1000-8000-00805f9b34fb", "Blood Pressure"},
            {"00001811-0000-1000-8000-00805f9b34fb", "Alert Notification Service"},
            {"00001812-0000-1000-8000-00805f9b34fb", "Human Interface Device"},
            {"00001816-0000-1000-8000-00805f9b34fb", "Cycling Speed and Cadence"},
            {"00001818-0000-1000-8000-00805f9b34fb", "Cycling Power"},
            {"0000181a-0000-1000-8000-00805f9b34fb", "Environmental Sensing"},
            {"0000181c-0000-1000-8000-00805f9b34fb", "User Data"},
            {"0000181d-0000-1000-8000-00805f9b34fb", "Weight Scale"}
        }

        Dim uuidString As String = uuid.ToString().ToLower()
        If knownServices.ContainsKey(uuidString) Then
            Return $"{knownServices(uuidString)} ({uuid})"
        Else
            Return uuid.ToString()
        End If
    End Function

    Private Async Sub lstServices_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstServices.SelectedIndexChanged
        Try
            If lstServices.SelectedIndex < 0 OrElse lstServices.SelectedIndex >= servicesList.Count Then
                lblCharacteristicsTitle.Text = "Select a service"
                cmbCharacteristics.Items.Clear()
                characteristicsList.Clear()
                txtCharDetails.Clear()
                Return
            End If

            Dim service As GattDeviceService = servicesList(lstServices.SelectedIndex)

            cmbCharacteristics.Items.Clear()
            characteristicsList.Clear()
            txtCharDetails.Clear()
            lblCharacteristicsTitle.Text = "Loading characteristics..."
            Application.DoEvents()

            Await LoadCharacteristicsAsync(service)

        Catch ex As Exception
            lblCharacteristicsTitle.Text = "Error in selection"
            MessageBox.Show($"Error in service selection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Function LoadCharacteristicsAsync(service As GattDeviceService) As Task
        Try
            cmbCharacteristics.Items.Clear()
            characteristicsList.Clear()
            txtCharDetails.Clear()
            lblCharacteristicsTitle.Text = "Loading..."
            Application.DoEvents()

            If service Is Nothing Then
                lblCharacteristicsTitle.Text = "Error: Service is null"
                Return
            End If

            ' Open the service
            Dim openStatus As GattOpenStatus = Await service.OpenAsync(GattSharingMode.SharedReadAndWrite)

            If openStatus <> GattOpenStatus.Success AndAlso openStatus <> GattOpenStatus.AlreadyOpened Then
                lblCharacteristicsTitle.Text = $"Failed to open service"
                txtCharDetails.Text = $"⚠ Failed to open service: {openStatus}" & vbCrLf & vbCrLf &
                                     "Try:" & vbCrLf &
                                     "1. Disconnect and reconnect" & vbCrLf &
                                     "2. Pair device in Windows Settings"
                Return
            End If

            Dim result = Await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached)

            If result.Status = GattCommunicationStatus.Success Then
                If result.Characteristics Is Nothing OrElse result.Characteristics.Count = 0 Then
                    lblCharacteristicsTitle.Text = "No characteristics"
                    txtCharDetails.Text = "⚠ This service has no characteristics"
                Else
                    lblCharacteristicsTitle.Text = $"Characteristics ({result.Characteristics.Count})"

                    For Each characteristic In result.Characteristics
                        Dim charName As String = GetCharacteristicName(characteristic.Uuid)
                        Dim properties As String = GetCharacteristicProperties(characteristic.CharacteristicProperties)

                        ' Add to dropdown
                        cmbCharacteristics.Items.Add($"{charName} - {properties}")
                        characteristicsList.Add(characteristic)
                    Next

                    If cmbCharacteristics.Items.Count > 0 Then
                        cmbCharacteristics.SelectedIndex = 0
                    End If
                End If
            Else
                lblCharacteristicsTitle.Text = $"Failed: {result.Status}"
                txtCharDetails.Text = $"⚠ Communication Status: {result.Status}"
                If result.ProtocolError.HasValue Then
                    txtCharDetails.Text &= vbCrLf & $"Protocol Error: 0x{result.ProtocolError.Value:X2}"
                End If
            End If

        Catch ex As Exception
            lblCharacteristicsTitle.Text = $"Error"
            txtCharDetails.Text = $"⚠ Exception: {ex.Message}"
        End Try
    End Function

    Private Sub cmbCharacteristics_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCharacteristics.SelectedIndexChanged
        If cmbCharacteristics.SelectedIndex < 0 OrElse cmbCharacteristics.SelectedIndex >= characteristicsList.Count Then
            txtCharDetails.Clear()
            btnSend.Enabled = False
            Return
        End If

        Dim characteristic = characteristicsList(cmbCharacteristics.SelectedIndex)
        Dim charName As String = GetCharacteristicName(characteristic.Uuid)
        Dim properties As String = GetCharacteristicProperties(characteristic.CharacteristicProperties)

        txtCharDetails.Text = $"Name: {charName}" & vbCrLf &
                             $"UUID: {characteristic.Uuid}" & vbCrLf &
                             $"Handle: {characteristic.AttributeHandle}" & vbCrLf &
                             $"Properties: {properties}" & vbCrLf & vbCrLf

        ' Enable send button if writable
        btnSend.Enabled = (characteristic.CharacteristicProperties And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write OrElse
                         (characteristic.CharacteristicProperties And GattCharacteristicProperties.WriteWithoutResponse) = GattCharacteristicProperties.WriteWithoutResponse

        If Not btnSend.Enabled Then
            txtCharDetails.Text &= "⚠ This characteristic is not writable"
        Else
            txtCharDetails.Text &= "✓ Ready to send data"
        End If
    End Sub

    Private Async Sub btnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        If cmbCharacteristics.SelectedIndex < 0 OrElse String.IsNullOrWhiteSpace(txtData.Text) Then
            MessageBox.Show("Please select a characteristic and enter data to send.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim characteristic = characteristicsList(cmbCharacteristics.SelectedIndex)
            Dim dataToSend As Byte()

            ' Try to parse as hex first
            If IsHexString(txtData.Text) Then
                dataToSend = HexStringToBytes(txtData.Text)
                txtCharDetails.Text &= vbCrLf & vbCrLf & $"Sending HEX: {txtData.Text}"
            Else
                ' Send as UTF-8 text
                dataToSend = System.Text.Encoding.UTF8.GetBytes(txtData.Text)
                txtCharDetails.Text &= vbCrLf & vbCrLf & $"Sending TEXT: {txtData.Text}"
            End If

            btnSend.Enabled = False
            lblCharacteristicsTitle.Text = "Sending..."
            Application.DoEvents()

            Dim writer = New Windows.Storage.Streams.DataWriter()
            writer.WriteBytes(dataToSend)

            Dim writeResult As GattCommunicationStatus

            If (characteristic.CharacteristicProperties And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write Then
                writeResult = Await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse)
            Else
                writeResult = Await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse)
            End If

            If writeResult = GattCommunicationStatus.Success Then
                lblCharacteristicsTitle.Text = "Send Successful!"
                txtCharDetails.Text &= vbCrLf & $"✓ Sent {dataToSend.Length} bytes successfully"
                MessageBox.Show($"Data sent successfully!{vbCrLf}{dataToSend.Length} bytes", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                lblCharacteristicsTitle.Text = "Send Failed"
                txtCharDetails.Text &= vbCrLf & $"✗ Failed: {writeResult}"
                MessageBox.Show($"Failed to send data: {writeResult}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            lblCharacteristicsTitle.Text = "Send Error"
            MessageBox.Show($"Error sending data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnSend.Enabled = True
            lblCharacteristicsTitle.Text = $"Characteristics ({characteristicsList.Count})"
        End Try
    End Sub

    Private Function IsHexString(text As String) As Boolean
        Dim cleanText = text.Replace(" ", "").Replace("-", "").Replace(":", "")
        If cleanText.Length Mod 2 <> 0 Then Return False

        For Each c As Char In cleanText
            If Not Uri.IsHexDigit(c) Then Return False
        Next

        Return cleanText.Length > 0
    End Function

    Private Function HexStringToBytes(hexString As String) As Byte()
        Dim cleanHex = hexString.Replace(" ", "").Replace("-", "").Replace(":", "")
        Dim bytes(cleanHex.Length \ 2 - 1) As Byte

        For i As Integer = 0 To bytes.Length - 1
            bytes(i) = Convert.ToByte(cleanHex.Substring(i * 2, 2), 16)
        Next

        Return bytes
    End Function

    Private Function GetCharacteristicName(uuid As Guid) As String
        Dim knownCharacteristics As New Dictionary(Of String, String) From {
            {"00002a00-0000-1000-8000-00805f9b34fb", "Device Name"},
            {"00002a01-0000-1000-8000-00805f9b34fb", "Appearance"},
            {"00002a19-0000-1000-8000-00805f9b34fb", "Battery Level"},
            {"00002a23-0000-1000-8000-00805f9b34fb", "System ID"},
            {"00002a24-0000-1000-8000-00805f9b34fb", "Model Number"},
            {"00002a25-0000-1000-8000-00805f9b34fb", "Serial Number"},
            {"00002a26-0000-1000-8000-00805f9b34fb", "Firmware Revision"},
            {"00002a27-0000-1000-8000-00805f9b34fb", "Hardware Revision"},
            {"00002a28-0000-1000-8000-00805f9b34fb", "Software Revision"},
            {"00002a29-0000-1000-8000-00805f9b34fb", "Manufacturer Name"},
            {"00002a37-0000-1000-8000-00805f9b34fb", "Heart Rate Measurement"},
            {"00002a38-0000-1000-8000-00805f9b34fb", "Body Sensor Location"}
        }

        Dim uuidString As String = uuid.ToString().ToLower()
        If knownCharacteristics.ContainsKey(uuidString) Then
            Return knownCharacteristics(uuidString)
        Else
            Return "Custom Characteristic"
        End If
    End Function

    Private Function GetCharacteristicProperties(props As GattCharacteristicProperties) As String
        Dim propList As New List(Of String)

        If (props And GattCharacteristicProperties.Read) = GattCharacteristicProperties.Read Then
            propList.Add("Read")
        End If
        If (props And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write Then
            propList.Add("Write")
        End If
        If (props And GattCharacteristicProperties.WriteWithoutResponse) = GattCharacteristicProperties.WriteWithoutResponse Then
            propList.Add("Write Without Response")
        End If
        If (props And GattCharacteristicProperties.Notify) = GattCharacteristicProperties.Notify Then
            propList.Add("Notify")
        End If
        If (props And GattCharacteristicProperties.Indicate) = GattCharacteristicProperties.Indicate Then
            propList.Add("Indicate")
        End If
        If (props And GattCharacteristicProperties.Broadcast) = GattCharacteristicProperties.Broadcast Then
            propList.Add("Broadcast")
        End If

        If propList.Count > 0 Then
            Return String.Join(", ", propList)
        Else
            Return "None"
        End If
    End Function

    Private Sub btnDisconnect_Click(sender As Object, e As EventArgs) Handles btnDisconnect.Click
        If currentDevice IsNot Nothing Then
            currentDevice.Dispose()
            currentDevice = Nothing
        End If

        Me.Close()
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Me.Close()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Remove event handler
        If currentDevice IsNot Nothing Then
            RemoveHandler currentDevice.ConnectionStatusChanged, AddressOf OnConnectionStatusChanged
        End If

        If gattSession IsNot Nothing Then
            gattSession.Dispose()
            gattSession = Nothing
        End If

        For Each service In servicesList
            service.Dispose()
        Next
        servicesList.Clear()

        MyBase.OnFormClosing(e)
    End Sub

End Class
