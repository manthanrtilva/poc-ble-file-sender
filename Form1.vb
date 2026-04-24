Imports Windows.Devices.Bluetooth
Imports Windows.Devices.Bluetooth.Advertisement
Imports Windows.Devices.Enumeration
Imports Windows.Devices.Bluetooth.GenericAttributeProfile

Public Class Form1
    Private bleWatcher As BluetoothLEAdvertisementWatcher
    Private discoveredDevices As New Dictionary(Of ULong, String)
    Private allDeviceItems As New List(Of String)
    Private connectedDevice As BluetoothLEDevice

    Public Sub New()
        InitializeComponent()
        InitializeBLEWatcher()
    End Sub

    Private Sub InitializeBLEWatcher()
        bleWatcher = New BluetoothLEAdvertisementWatcher()
        bleWatcher.ScanningMode = BluetoothLEScanningMode.Active

        AddHandler bleWatcher.Received, AddressOf OnAdvertisementReceived
        AddHandler bleWatcher.Stopped, AddressOf OnWatcherStopped
    End Sub

    Private Async Sub btnScan_Click(sender As Object, e As EventArgs) Handles btnScan.Click
        If bleWatcher.Status = BluetoothLEAdvertisementWatcherStatus.Started Then
            bleWatcher.Stop()
            btnScan.Text = "Scan BLE Devices"
            lblStatus.Text = "Scan stopped"
        Else
            discoveredDevices.Clear()
            lstDevices.Items.Clear()
            lblStatus.Text = "Scanning..."
            btnScan.Text = "Stop Scanning"

            bleWatcher.Start()
        End If
    End Sub

    Private Async Sub OnAdvertisementReceived(sender As BluetoothLEAdvertisementWatcher, args As BluetoothLEAdvertisementReceivedEventArgs)
        Dim deviceAddress As ULong = args.BluetoothAddress
        Dim deviceName As String = args.Advertisement.LocalName

        If String.IsNullOrEmpty(deviceName) Then
            Try
                Dim device = Await BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress)
                If device IsNot Nothing Then
                    deviceName = device.Name
                    device.Dispose()
                End If
            Catch ex As Exception
            End Try
        End If

        If String.IsNullOrEmpty(deviceName) Then
            deviceName = "Unknown Device"
        End If

        Dim addressString As String = deviceAddress.ToString("X12")
        Dim formattedAddress As String = String.Format("{0}:{1}:{2}:{3}:{4}:{5}",
            addressString.Substring(0, 2),
            addressString.Substring(2, 2),
            addressString.Substring(4, 2),
            addressString.Substring(6, 2),
            addressString.Substring(8, 2),
            addressString.Substring(10, 2))

        If Not discoveredDevices.ContainsKey(deviceAddress) Then
            discoveredDevices.Add(deviceAddress, deviceName)

            Dim deviceItem As String = $"{deviceName} ({formattedAddress}) - RSSI: {args.RawSignalStrengthInDBm} dBm"

            If lstDevices.InvokeRequired Then
                lstDevices.Invoke(Sub()
                                      allDeviceItems.Add(deviceItem)
                                      If String.IsNullOrWhiteSpace(txtSearch.Text) OrElse
                                         deviceItem.ToLower().Contains(txtSearch.Text.ToLower()) Then
                                          lstDevices.Items.Add(deviceItem)
                                      End If
                                      UpdateStatusLabel()
                                  End Sub)
            Else
                allDeviceItems.Add(deviceItem)
                If String.IsNullOrWhiteSpace(txtSearch.Text) OrElse
                   deviceItem.ToLower().Contains(txtSearch.Text.ToLower()) Then
                    lstDevices.Items.Add(deviceItem)
                End If
                UpdateStatusLabel()
            End If
        End If
    End Sub

    Private Sub OnWatcherStopped(sender As BluetoothLEAdvertisementWatcher, args As BluetoothLEAdvertisementWatcherStoppedEventArgs)
        If btnScan.InvokeRequired Then
            btnScan.Invoke(Sub()
                               btnScan.Text = "Scan BLE Devices"
                               UpdateStatusLabel("Scan stopped. ")
                           End Sub)
        Else
            btnScan.Text = "Scan BLE Devices"
            UpdateStatusLabel("Scan stopped. ")
        End If
    End Sub

    Private Sub UpdateStatusLabel(Optional prefix As String = "")
        Dim displayed As Integer = lstDevices.Items.Count
        Dim total As Integer = discoveredDevices.Count

        If displayed = total Then
            lblStatus.Text = $"{prefix}Found {total} device(s)"
        Else
            lblStatus.Text = $"{prefix}Showing {displayed} of {total} device(s)"
        End If
    End Sub

    Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
        FilterDeviceList()
    End Sub

    Private Sub FilterDeviceList()
        lstDevices.Items.Clear()

        Dim searchText As String = txtSearch.Text.ToLower()

        If String.IsNullOrWhiteSpace(searchText) Then
            For Each deviceItem In allDeviceItems
                lstDevices.Items.Add(deviceItem)
            Next
        Else
            For Each deviceItem In allDeviceItems
                If deviceItem.ToLower().Contains(searchText) Then
                    lstDevices.Items.Add(deviceItem)
                End If
            Next
        End If

        UpdateStatusLabel()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If bleWatcher IsNot Nothing AndAlso bleWatcher.Status = BluetoothLEAdvertisementWatcherStatus.Started Then
            bleWatcher.Stop()
        End If

        If connectedDevice IsNot Nothing Then
            connectedDevice.Dispose()
            connectedDevice = Nothing
        End If

        MyBase.OnFormClosing(e)
    End Sub

    Private Async Sub lstDevices_DoubleClick(sender As Object, e As EventArgs) Handles lstDevices.DoubleClick
        If lstDevices.SelectedItem Is Nothing Then
            Return
        End If

        Dim selectedItem As String = lstDevices.SelectedItem.ToString()
        Dim deviceAddress As ULong = GetDeviceAddressFromListItem(selectedItem)

        If deviceAddress = 0 Then
            MessageBox.Show("Unable to extract device address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        lblStatus.Text = "Connecting..."
        btnScan.Enabled = False

        Try
            If connectedDevice IsNot Nothing Then
                connectedDevice.Dispose()
                connectedDevice = Nothing
            End If

            lblStatus.Text = "Connecting to device..."
            connectedDevice = Await BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress)

            If connectedDevice Is Nothing Then
                MessageBox.Show("Failed to get device reference.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                lblStatus.Text = "Connection failed"
                btnScan.Enabled = True
                Return
            End If

            lblStatus.Text = "Establishing connection..."
            Dim gattResult = Await connectedDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached)

            If gattResult.Status = GattCommunicationStatus.Success Then
                lblStatus.Text = "Activating GATT session..."
                Application.DoEvents()

                ' CRITICAL: We MUST perform an actual GATT READ to prevent disconnect
                ' Just getting the list doesn't trigger device communication
                Dim gattActivated As Boolean = False

                For Each service In gattResult.Services
                    Try
                        ' Open the service
                        Dim openResult = Await service.OpenAsync(GattSharingMode.SharedReadAndWrite)

                        If openResult = GattOpenStatus.Success OrElse openResult = GattOpenStatus.AlreadyOpened Then
                            ' Get characteristics
                            Dim charsResult = Await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached)

                            If charsResult.Status = GattCommunicationStatus.Success AndAlso charsResult.Characteristics.Count > 0 Then
                                ' Find a readable characteristic and READ it to trigger GATT activity
                                For Each characteristic In charsResult.Characteristics
                                    If (characteristic.CharacteristicProperties And GattCharacteristicProperties.Read) = GattCharacteristicProperties.Read Then
                                        Try
                                            lblStatus.Text = $"Reading from device..."
                                            Application.DoEvents()

                                            ' THIS is what triggers gattc_read_req_ind_handler on the device!
                                            Dim readResult = Await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached)

                                            If readResult.Status = GattCommunicationStatus.Success Then
                                                gattActivated = True
                                                lblStatus.Text = "GATT communication established!"
                                                Application.DoEvents()
                                                Await Task.Delay(200) ' Give device time to process
                                                Exit For
                                            End If
                                        Catch ex As Exception
                                            ' Try next characteristic
                                            Continue For
                                        End Try
                                    End If
                                Next

                                If gattActivated Then
                                    Exit For
                                End If
                            End If
                        End If
                    Catch ex As Exception
                        ' Try next service
                        Continue For
                    End Try
                Next

                If Not gattActivated Then
                    MessageBox.Show("Failed to establish GATT communication with device." & vbCrLf & vbCrLf &
                                  "The device requires pairing or doesn't have readable characteristics." & vbCrLf & vbCrLf &
                                  "Please pair the device in Windows Bluetooth settings first.", 
                                  "GATT Communication Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    lblStatus.Text = "GATT activation failed"
                    connectedDevice.Dispose()
                    connectedDevice = Nothing
                    btnScan.Enabled = True
                    Return
                End If

                lblStatus.Text = $"Connected to {If(String.IsNullOrEmpty(connectedDevice.Name), "Unknown Device", connectedDevice.Name)}"

                ' Now open the services form - GATT is already active
                Dim servicesForm As New DeviceServicesForm(connectedDevice, gattResult.Services)

                ' Important: Don't dispose the device here, let the form manage it
                Dim tempDevice = connectedDevice
                connectedDevice = Nothing ' Remove our reference so form can manage it

                servicesForm.ShowDialog()

                ' After form closes, dispose the device
                If tempDevice IsNot Nothing Then
                    tempDevice.Dispose()
                End If

                lblStatus.Text = "Disconnected"
            ElseIf gattResult.Status = GattCommunicationStatus.Unreachable Then
                MessageBox.Show("Device is not reachable. Please make sure the device is turned on and nearby.", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                lblStatus.Text = "Device unreachable"
                connectedDevice.Dispose()
                connectedDevice = Nothing
            ElseIf gattResult.Status = GattCommunicationStatus.ProtocolError Then
                MessageBox.Show("Protocol error occurred while connecting.", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                lblStatus.Text = "Protocol error"
                connectedDevice.Dispose()
                connectedDevice = Nothing
            Else
                MessageBox.Show($"Failed to connect: {gattResult.Status}", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                lblStatus.Text = $"Connection failed: {gattResult.Status}"
                connectedDevice.Dispose()
                connectedDevice = Nothing
            End If

        Catch ex As UnauthorizedAccessException
            MessageBox.Show("Access denied. Please check Bluetooth permissions in Windows Settings.", "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = "Access denied"
        Catch ex As Exception
            MessageBox.Show($"Error connecting to device: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = "Connection error"
        Finally
            btnScan.Enabled = True
        End Try
    End Sub

    Private Function GetDeviceAddressFromListItem(item As String) As ULong
        Try
            Dim startIndex As Integer = item.IndexOf("(") + 1
            Dim endIndex As Integer = item.IndexOf(")")

            If startIndex > 0 AndAlso endIndex > startIndex Then
                Dim addressPart As String = item.Substring(startIndex, endIndex - startIndex)
                Dim cleanAddress As String = addressPart.Replace(":", "")

                Return Convert.ToUInt64(cleanAddress, 16)
            End If
        Catch ex As Exception
        End Try

        Return 0
    End Function

End Class
