Imports Windows.Devices.Bluetooth
Imports Windows.Devices.Bluetooth.GenericAttributeProfile

Public Class DeviceServicesForm
    Private currentDevice As BluetoothLEDevice
    Private servicesList As New List(Of GattDeviceService)
    Private characteristicsList As New List(Of GattCharacteristic)
    Private gattSession As GattSession
    Private selectedFilePath As String = ""

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
            btnSendFile.Enabled = False
            Return
        End If

        Dim characteristic = characteristicsList(cmbCharacteristics.SelectedIndex)
        Dim charName As String = GetCharacteristicName(characteristic.Uuid)
        Dim properties As String = GetCharacteristicProperties(characteristic.CharacteristicProperties)

        txtCharDetails.Text = $"Name: {charName}" & vbCrLf &
                             $"UUID: {characteristic.Uuid}" & vbCrLf &
                             $"Handle: {characteristic.AttributeHandle}" & vbCrLf &
                             $"Properties: {properties}" & vbCrLf & vbCrLf

        ' Enable send buttons if writable
        Dim isWritable = (characteristic.CharacteristicProperties And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write OrElse
                        (characteristic.CharacteristicProperties And GattCharacteristicProperties.WriteWithoutResponse) = GattCharacteristicProperties.WriteWithoutResponse

        btnSend.Enabled = isWritable
        btnSendFile.Enabled = Not String.IsNullOrEmpty(selectedFilePath) ' File send doesn't require characteristic selection

        If Not isWritable Then
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

            txtCharDetails.Text &= vbCrLf & $"Bytes: {BitConverter.ToString(dataToSend).Replace("-", " ")}"

            btnSend.Enabled = False
            lblCharacteristicsTitle.Text = "Sending..."
            Application.DoEvents()

            Dim writer = New Windows.Storage.Streams.DataWriter()
            writer.WriteBytes(dataToSend)

            Dim writeResult As GattCommunicationStatus
            Dim useWriteWithResponse As Boolean = (characteristic.CharacteristicProperties And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write

            If useWriteWithResponse Then
                writeResult = Await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse)
                txtCharDetails.Text &= vbCrLf & "Write Mode: WriteWithResponse"
            Else
                writeResult = Await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse)
                txtCharDetails.Text &= vbCrLf & "Write Mode: WriteWithoutResponse"
            End If

            txtCharDetails.Text &= vbCrLf & $"Write Status: {writeResult}"

            If writeResult = GattCommunicationStatus.Success Then
                lblCharacteristicsTitle.Text = "Send Successful!"
                txtCharDetails.Text &= vbCrLf & $"✓ Sent {dataToSend.Length} bytes successfully"

                ' If write was successful and characteristic is readable, try to read response
                If (characteristic.CharacteristicProperties And GattCharacteristicProperties.Read) = GattCharacteristicProperties.Read Then
                    txtCharDetails.Text &= vbCrLf & vbCrLf & "Reading response..."
                    Application.DoEvents()

                    Try
                        Dim readResult = Await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached)

                        If readResult.Status = GattCommunicationStatus.Success Then
                            Dim reader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value)
                            Dim responseBytes(reader.UnconsumedBufferLength - 1) As Byte
                            reader.ReadBytes(responseBytes)

                            txtCharDetails.Text &= vbCrLf & $"Response Length: {responseBytes.Length} bytes"
                            txtCharDetails.Text &= vbCrLf & $"Response HEX: {BitConverter.ToString(responseBytes).Replace("-", " ")}"

                            ' Try to interpret as text if printable
                            If IsPrintableAscii(responseBytes) Then
                                Dim responseText = System.Text.Encoding.UTF8.GetString(responseBytes)
                                txtCharDetails.Text &= vbCrLf & $"Response TEXT: {responseText}"
                            End If

                            MessageBox.Show($"Data sent successfully!{vbCrLf}{dataToSend.Length} bytes sent{vbCrLf}{vbCrLf}Response: {responseBytes.Length} bytes{vbCrLf}HEX: {BitConverter.ToString(responseBytes).Replace("-", " ")}",
                                          "Success with Response", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Else
                            txtCharDetails.Text &= vbCrLf & $"Read response failed: {readResult.Status}"
                            MessageBox.Show($"Data sent successfully!{vbCrLf}{dataToSend.Length} bytes", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    Catch ex As Exception
                        txtCharDetails.Text &= vbCrLf & $"Could not read response: {ex.Message}"
                        MessageBox.Show($"Data sent successfully!{vbCrLf}{dataToSend.Length} bytes", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End Try
                Else
                    MessageBox.Show($"Data sent successfully!{vbCrLf}{dataToSend.Length} bytes", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
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

    Private Function IsPrintableAscii(data As Byte()) As Boolean
        For Each b As Byte In data
            If b < 32 AndAlso b <> 10 AndAlso b <> 13 AndAlso b <> 9 Then ' Allow LF, CR, TAB
                Return False
            End If
            If b > 126 Then
                Return False
            End If
        Next
        Return True
    End Function

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

    Private Sub btnBrowseFile_Click(sender As Object, e As EventArgs) Handles btnBrowseFile.Click
        Using openFileDialog As New OpenFileDialog()
            openFileDialog.Title = "Select File to Send"
            openFileDialog.Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt|Binary Files (*.bin)|*.bin"
            openFileDialog.FilterIndex = 1

            If openFileDialog.ShowDialog() = DialogResult.OK Then
                selectedFilePath = openFileDialog.FileName
                txtFilePath.Text = selectedFilePath

                ' Show file info
                Dim fileInfo As New IO.FileInfo(selectedFilePath)
                txtCharDetails.Text &= vbCrLf & vbCrLf & $"File selected: {fileInfo.Name}" & vbCrLf &
                                      $"Size: {fileInfo.Length} bytes" & vbCrLf &
                                      $"Last modified: {fileInfo.LastWriteTime}"

                btnSendFile.Enabled = True
            End If
        End Using
    End Sub

    Private Async Sub btnSendFile_Click(sender As Object, e As EventArgs) Handles btnSendFile.Click
        If cmbCharacteristics.SelectedIndex < 0 Then
            MessageBox.Show("Please select a characteristic first.", "No Characteristic", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If String.IsNullOrEmpty(selectedFilePath) OrElse Not IO.File.Exists(selectedFilePath) Then
            MessageBox.Show("Please select a valid file first.", "No File", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim characteristic = characteristicsList(cmbCharacteristics.SelectedIndex)

            ' Check if writable
            If Not ((characteristic.CharacteristicProperties And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write OrElse
                   (characteristic.CharacteristicProperties And GattCharacteristicProperties.WriteWithoutResponse) = GattCharacteristicProperties.WriteWithoutResponse) Then
                MessageBox.Show("Selected characteristic is not writable.", "Cannot Write", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Read file
            Dim fileBytes As Byte() = IO.File.ReadAllBytes(selectedFilePath)
            Dim fileInfo As New IO.FileInfo(selectedFilePath)

            ' Pad data to multiple of 4 bytes for CRC calculation
            Dim paddedBytes As Byte() = PadToMultipleOf4(fileBytes)

            ' Calculate CRC32 on padded data
            Dim crc32Value As UInteger = CalculateCRC32(paddedBytes)
            Dim fileSize As UInteger = CUInt(fileBytes.Length) ' Original file size (not padded)

            txtCharDetails.Text &= vbCrLf & vbCrLf & $"File: {fileInfo.Name}" & vbCrLf &
                                  $"Size: {fileSize} bytes" & vbCrLf &
                                  $"Padded Size: {paddedBytes.Length} bytes" & vbCrLf &
                                  $"CRC32: 0x{crc32Value:X8}"

            ' Find two control characteristics
            ' Metadata: 11110004-1111-1111-1111-111111111111
            ' File Data: 11110005-1111-1111-1111-111111111111
            Dim metadataCharUuid As New Guid("11110004-1111-1111-1111-111111111111")
            Dim fileDataCharUuid As New Guid("11110005-1111-1111-1111-111111111111")
            Dim metadataChar As GattCharacteristic = Nothing
            Dim fileDataChar As GattCharacteristic = Nothing

            lblCharacteristicsTitle.Text = "Finding control characteristics..."
            Application.DoEvents()

            ' Search through all services for both characteristics
            For Each service In servicesList
                Try
                    Dim charsResult = Await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached)
                    If charsResult.Status = GattCommunicationStatus.Success Then
                        For Each ch In charsResult.Characteristics
                            If ch.Uuid = metadataCharUuid Then
                                metadataChar = ch
                            ElseIf ch.Uuid = fileDataCharUuid Then
                                fileDataChar = ch
                            End If
                        Next
                    End If
                    If metadataChar IsNot Nothing AndAlso fileDataChar IsNot Nothing Then Exit For
                Catch ex As Exception
                    ' Continue searching
                End Try
            Next

            If metadataChar Is Nothing Then
                MessageBox.Show($"Metadata characteristic not found!{vbCrLf}{vbCrLf}UUID: {metadataCharUuid}{vbCrLf}{vbCrLf}Cannot send file.",
                              "Metadata Characteristic Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            If fileDataChar Is Nothing Then
                MessageBox.Show($"File data characteristic not found!{vbCrLf}{vbCrLf}UUID: {fileDataCharUuid}{vbCrLf}{vbCrLf}Cannot send file.",
                              "File Data Characteristic Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            txtCharDetails.Text &= vbCrLf & $"Found metadata characteristic: {metadataCharUuid}"
            txtCharDetails.Text &= vbCrLf & $"Found file data characteristic: {fileDataCharUuid}"

            ' Send [filesize, crc32] to metadata characteristic (11110004)
            lblCharacteristicsTitle.Text = "Sending file metadata..."
            txtCharDetails.Text &= vbCrLf & vbCrLf & "Sending metadata to characteristic 11110004..."
            Application.DoEvents()

            Dim metadataWriter = New Windows.Storage.Streams.DataWriter()
            metadataWriter.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian
            metadataWriter.WriteUInt32(fileSize)   ' 4 bytes: file size
            metadataWriter.WriteUInt32(crc32Value) ' 4 bytes: CRC32

            ' Log metadata bytes
            Dim metadataBytes(7) As Byte
            Array.Copy(BitConverter.GetBytes(fileSize), 0, metadataBytes, 0, 4)
            Array.Copy(BitConverter.GetBytes(crc32Value), 0, metadataBytes, 4, 4)
            txtCharDetails.Text &= vbCrLf & $"Metadata Bytes: {BitConverter.ToString(metadataBytes).Replace("-", " ")}"

            Dim metadataResult As GattCommunicationStatus
            Dim metadataBuffer = metadataWriter.DetachBuffer()

            If (metadataChar.CharacteristicProperties And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write Then
                metadataResult = Await metadataChar.WriteValueAsync(metadataBuffer, GattWriteOption.WriteWithResponse)
                txtCharDetails.Text &= vbCrLf & "Metadata Write Mode: WriteWithResponse"
            Else
                metadataResult = Await metadataChar.WriteValueAsync(metadataBuffer, GattWriteOption.WriteWithoutResponse)
                txtCharDetails.Text &= vbCrLf & "Metadata Write Mode: WriteWithoutResponse"
            End If

            txtCharDetails.Text &= vbCrLf & $"Metadata Write Status: {metadataResult}"

            If metadataResult <> GattCommunicationStatus.Success Then
                MessageBox.Show($"Failed to send metadata: {metadataResult}", "Metadata Send Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                txtCharDetails.Text &= vbCrLf & $"✗ Metadata send failed: {metadataResult}"
                Return
            End If

            txtCharDetails.Text &= vbCrLf & $"✓ Metadata sent: Size={fileSize}, CRC32=0x{crc32Value:X8}"

            ' Try to read response from metadata characteristic if readable
            If (metadataChar.CharacteristicProperties And GattCharacteristicProperties.Read) = GattCharacteristicProperties.Read Then
                txtCharDetails.Text &= vbCrLf & "Reading metadata response..."
                Application.DoEvents()

                Try
                    Await Task.Delay(50) ' Small delay for device to process
                    Dim readResult = Await metadataChar.ReadValueAsync(BluetoothCacheMode.Uncached)

                    If readResult.Status = GattCommunicationStatus.Success Then
                        Dim reader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value)
                        Dim responseBytes(reader.UnconsumedBufferLength - 1) As Byte
                        reader.ReadBytes(responseBytes)

                        txtCharDetails.Text &= vbCrLf & $"Metadata Response: {BitConverter.ToString(responseBytes).Replace("-", " ")}"

                        ' Interpret response if it's a status byte
                        If responseBytes.Length > 0 Then
                            txtCharDetails.Text &= vbCrLf & $"Response Value: 0x{responseBytes(0):X2} ({responseBytes(0)})"
                        End If
                    Else
                        txtCharDetails.Text &= vbCrLf & $"Could not read metadata response: {readResult.Status}"
                    End If
                Catch ex As Exception
                    txtCharDetails.Text &= vbCrLf & $"Metadata response read error: {ex.Message}"
                End Try
            End If

            Await Task.Delay(100) ' Give device time to process metadata

            ' Confirm large file send
            If fileBytes.Length > 512 Then
                Dim result = MessageBox.Show($"File size is {fileBytes.Length} bytes. This will be sent in chunks." & vbCrLf & vbCrLf &
                                            "Metadata has been sent. Continue with file transfer?",
                                            "Large File", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result <> DialogResult.Yes Then
                    Return
                End If
            End If

            btnSendFile.Enabled = False
            btnBrowseFile.Enabled = False
            lblCharacteristicsTitle.Text = "Sending file data..."

            ' Get MTU size (typical BLE MTU is 20-512 bytes, we'll use conservative 20)
            Dim chunkSize As Integer = 20
            Dim totalChunks As Integer = Math.Ceiling(fileBytes.Length / chunkSize)
            Dim bytesSent As Integer = 0

            txtCharDetails.Text &= vbCrLf & vbCrLf & $"Sending file data to characteristic 11110005..." & vbCrLf &
                                  $"Total chunks: {totalChunks}"

            For chunkIndex As Integer = 0 To totalChunks - 1
                Dim offset As Integer = chunkIndex * chunkSize
                Dim currentChunkSize As Integer = Math.Min(chunkSize, fileBytes.Length - offset)
                Dim chunk(currentChunkSize - 1) As Byte

                Array.Copy(fileBytes, offset, chunk, 0, currentChunkSize)

                ' Send chunk to FILE DATA CHARACTERISTIC (11110005)
                Dim writer = New Windows.Storage.Streams.DataWriter()
                writer.WriteBytes(chunk)

                Dim writeResult As GattCommunicationStatus

                If (fileDataChar.CharacteristicProperties And GattCharacteristicProperties.Write) = GattCharacteristicProperties.Write Then
                    writeResult = Await fileDataChar.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse)
                Else
                    writeResult = Await fileDataChar.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse)
                End If

                If writeResult <> GattCommunicationStatus.Success Then
                    MessageBox.Show($"Failed to send chunk {chunkIndex + 1}/{totalChunks}: {writeResult}", "Send Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    txtCharDetails.Text &= vbCrLf & $"✗ Failed at chunk {chunkIndex + 1}/{totalChunks}: {writeResult}"
                    Return
                End If

                bytesSent += currentChunkSize
                Await Task.Delay(10)

                ' Try to read response from file data characteristic after each write
                'If (fileDataChar.CharacteristicProperties And GattCharacteristicProperties.Read) = GattCharacteristicProperties.Read Then
                '    Try
                '        Await Task.Delay(10) ' Small delay for device to prepare response
                '        Dim readResult = Await fileDataChar.ReadValueAsync(BluetoothCacheMode.Uncached)

                '        txtCharDetails.Text &= vbCrLf & $"Chunk {chunkIndex + 1}/{totalChunks} Read Status: {readResult.Status}"

                '        If readResult.Status = GattCommunicationStatus.Success Then
                '            Dim reader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value)
                '            txtCharDetails.Text &= vbCrLf & $"Chunk {chunkIndex + 1}/{totalChunks} Buffer Length: {reader.UnconsumedBufferLength}"

                '            If reader.UnconsumedBufferLength > 0 Then
                '                Dim responseBytes(reader.UnconsumedBufferLength - 1) As Byte
                '                reader.ReadBytes(responseBytes)

                '                txtCharDetails.Text &= vbCrLf & $"Chunk {chunkIndex + 1}/{totalChunks} Response: {BitConverter.ToString(responseBytes).Replace("-", " ")}"
                '            Else
                '                txtCharDetails.Text &= vbCrLf & $"Chunk {chunkIndex + 1}/{totalChunks} Response: (empty buffer)"
                '            End If
                '        End If
                '    Catch ex As Exception
                '        txtCharDetails.Text &= vbCrLf & $"Chunk {chunkIndex + 1}/{totalChunks} Read Error: {ex.Message}"
                '    End Try
                'Else
                '    txtCharDetails.Text &= vbCrLf & $"Chunk {chunkIndex + 1}/{totalChunks} - Characteristic not readable"
                'End If

                lblCharacteristicsTitle.Text = $"Sending... {bytesSent}/{fileBytes.Length} bytes ({chunkIndex + 1}/{totalChunks})"
                Application.DoEvents()

                ' Small delay between chunks to avoid overwhelming device
                If (fileDataChar.CharacteristicProperties And GattCharacteristicProperties.Write) <> GattCharacteristicProperties.Write Then
                    Await Task.Delay(10) ' Delay for WriteWithoutResponse
                End If
            Next

            lblCharacteristicsTitle.Text = "File Sent Successfully!"
            txtCharDetails.Text &= vbCrLf & $"✓ Successfully sent {bytesSent} bytes in {totalChunks} chunks"

            ' Try to read final response from metadata characteristic
            If (metadataChar.CharacteristicProperties And GattCharacteristicProperties.Read) = GattCharacteristicProperties.Read Then
                txtCharDetails.Text &= vbCrLf & vbCrLf & "Reading final response from metadata characteristic..."
                Application.DoEvents()

                Try
                    Await Task.Delay(100) ' Wait for device to process
                    Dim readResult = Await metadataChar.ReadValueAsync(BluetoothCacheMode.Uncached)

                    If readResult.Status = GattCommunicationStatus.Success Then
                        Dim reader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value)
                        Dim responseBytes(reader.UnconsumedBufferLength - 1) As Byte
                        reader.ReadBytes(responseBytes)

                        txtCharDetails.Text &= vbCrLf & $"Final Response: {BitConverter.ToString(responseBytes).Replace("-", " ")}"

                        If responseBytes.Length > 0 Then
                            txtCharDetails.Text &= vbCrLf & $"Response Value: 0x{responseBytes(0):X2} ({responseBytes(0)})"
                        End If
                    End If
                Catch ex As Exception
                    txtCharDetails.Text &= vbCrLf & $"Final response read error: {ex.Message}"
                End Try
            End If

            MessageBox.Show($"File sent successfully!{vbCrLf}{vbCrLf}File: {fileInfo.Name}{vbCrLf}Size: {bytesSent} bytes{vbCrLf}CRC32: 0x{crc32Value:X8}{vbCrLf}Chunks: {totalChunks}{vbCrLf}Metadata: 11110004{vbCrLf}File Data: 11110005",
                          "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            lblCharacteristicsTitle.Text = "Send Error"
            txtCharDetails.Text &= vbCrLf & $"✗ Error: {ex.Message}"
            MessageBox.Show($"Error sending file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnSendFile.Enabled = True
            btnBrowseFile.Enabled = True
            lblCharacteristicsTitle.Text = $"Characteristics ({characteristicsList.Count})"
        End Try
    End Sub

    Private Function PadToMultipleOf4(data As Byte()) As Byte()
        Dim remainder As Integer = data.Length Mod 4

        If remainder = 0 Then
            ' Already multiple of 4, return as-is
            Return data
        End If

        ' Calculate padding needed
        Dim paddingNeeded As Integer = 4 - remainder
        Dim paddedData(data.Length + paddingNeeded - 1) As Byte

        ' Copy original data
        Array.Copy(data, paddedData, data.Length)

        ' Padding bytes are already 0 (default value for Byte array)
        ' But explicitly set them for clarity
        For i As Integer = data.Length To paddedData.Length - 1
            paddedData(i) = 0
        Next

        Return paddedData
    End Function

    Private Function CalculateCRC32(data As Byte()) As UInteger
        ' CRC32 polynomial (MPEG-2, same as hardware CRC on N32WB03x)
        Const polynomial As UInteger = &H4C11DB7UI

        ' Build CRC table (non-reflected)
        Dim crcTable(255) As UInteger
        For i As UInteger = 0 To 255
            Dim crc As UInteger = i << 24
            For j As Integer = 0 To 7
                If (crc And &H80000000UI) <> 0 Then
                    crc = (crc << 1) Xor polynomial
                Else
                    crc <<= 1
                End If
            Next
            crcTable(i) = crc
        Next

        ' Calculate CRC32 (MPEG-2: init=0xFFFFFFFF, no final XOR)
        Dim crc32 As UInteger = &HFFFFFFFFUI
        For Each b As Byte In data
            Dim index As Byte = CByte((crc32 >> 24) Xor b)
            crc32 = (crc32 << 8) Xor crcTable(index)
        Next

        Return crc32
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
