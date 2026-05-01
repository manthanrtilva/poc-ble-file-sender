<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class DeviceServicesForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        lblDeviceName = New Label()
        lblDeviceAddress = New Label()
        lblConnectionStatus = New Label()
        lstServices = New ListBox()
        lblServicesTitle = New Label()
        btnDisconnect = New Button()
        cmbCharacteristics = New ComboBox()
        lblCharacteristicsTitle = New Label()
        btnBack = New Button()
        txtData = New TextBox()
        btnSend = New Button()
        lblDataToSend = New Label()
        txtCharDetails = New TextBox()
        lblCharDetails = New Label()
        txtFilePath = New TextBox()
        btnBrowseFile = New Button()
        lblFile = New Label()
        btnSendFile = New Button()
        SuspendLayout()
        '
        'lblDeviceName
        '
        lblDeviceName.AutoSize = True
        lblDeviceName.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        lblDeviceName.Location = New Point(12, 9)
        lblDeviceName.Name = "lblDeviceName"
        lblDeviceName.Size = New Size(110, 21)
        lblDeviceName.TabIndex = 0
        lblDeviceName.Text = "Device Name"
        '
        'lblDeviceAddress
        '
        lblDeviceAddress.AutoSize = True
        lblDeviceAddress.Location = New Point(12, 35)
        lblDeviceAddress.Name = "lblDeviceAddress"
        lblDeviceAddress.Size = New Size(52, 15)
        lblDeviceAddress.TabIndex = 1
        lblDeviceAddress.Text = "Address:"
        '
        'lblConnectionStatus
        '
        lblConnectionStatus.AutoSize = True
        lblConnectionStatus.ForeColor = Color.Green
        lblConnectionStatus.Location = New Point(12, 55)
        lblConnectionStatus.Name = "lblConnectionStatus"
        lblConnectionStatus.Size = New Size(42, 15)
        lblConnectionStatus.TabIndex = 2
        lblConnectionStatus.Text = "Status:"
        '
        'lstServices
        '
        lstServices.FormattingEnabled = True
        lstServices.ItemHeight = 15
        lstServices.Location = New Point(12, 105)
        lstServices.Name = "lstServices"
        lstServices.Size = New Size(380, 424)
        lstServices.TabIndex = 3
        '
        'lblServicesTitle
        '
        lblServicesTitle.AutoSize = True
        lblServicesTitle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblServicesTitle.Location = New Point(12, 85)
        lblServicesTitle.Name = "lblServicesTitle"
        lblServicesTitle.Size = New Size(124, 15)
        lblServicesTitle.TabIndex = 4
        lblServicesTitle.Text = "Available Services (0)"
        '
        'btnDisconnect
        '
        btnDisconnect.Location = New Point(671, 12)
        btnDisconnect.Name = "btnDisconnect"
        btnDisconnect.Size = New Size(120, 35)
        btnDisconnect.TabIndex = 5
        btnDisconnect.Text = "Disconnect"
        btnDisconnect.UseVisualStyleBackColor = True
        '
        'cmbCharacteristics
        '
        cmbCharacteristics.DropDownStyle = ComboBoxStyle.DropDownList
        cmbCharacteristics.FormattingEnabled = True
        cmbCharacteristics.Location = New Point(410, 105)
        cmbCharacteristics.Name = "cmbCharacteristics"
        cmbCharacteristics.Size = New Size(380, 23)
        cmbCharacteristics.TabIndex = 6
        '
        'lblCharacteristicsTitle
        '
        lblCharacteristicsTitle.AutoSize = True
        lblCharacteristicsTitle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblCharacteristicsTitle.Location = New Point(410, 85)
        lblCharacteristicsTitle.Name = "lblCharacteristicsTitle"
        lblCharacteristicsTitle.Size = New Size(178, 15)
        lblCharacteristicsTitle.TabIndex = 7
        lblCharacteristicsTitle.Text = "Characteristics (Select a service)"
        '
        'btnBack
        '
        btnBack.Location = New Point(545, 12)
        btnBack.Name = "btnBack"
        btnBack.Size = New Size(120, 35)
        btnBack.TabIndex = 8
        btnBack.Text = "Back to Scan"
        btnBack.UseVisualStyleBackColor = True
        '
        'txtData
        '
        txtData.Location = New Point(410, 355)
        txtData.Multiline = True
        txtData.Name = "txtData"
        txtData.PlaceholderText = "Enter hex data (e.g., 48656C6C6F) or text"
        txtData.Size = New Size(280, 60)
        txtData.TabIndex = 9
        '
        'btnSend
        '
        btnSend.Location = New Point(696, 355)
        btnSend.Name = "btnSend"
        btnSend.Size = New Size(94, 60)
        btnSend.TabIndex = 10
        btnSend.Text = "Send Data"
        btnSend.UseVisualStyleBackColor = True
        '
        'lblDataToSend
        '
        lblDataToSend.AutoSize = True
        lblDataToSend.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblDataToSend.Location = New Point(410, 337)
        lblDataToSend.Name = "lblDataToSend"
        lblDataToSend.Size = New Size(78, 15)
        lblDataToSend.TabIndex = 11
        lblDataToSend.Text = "Data to Send"
        '
        'txtCharDetails
        '
        txtCharDetails.Location = New Point(410, 155)
        txtCharDetails.Multiline = True
        txtCharDetails.Name = "txtCharDetails"
        txtCharDetails.ReadOnly = True
        txtCharDetails.ScrollBars = ScrollBars.Vertical
        txtCharDetails.Size = New Size(380, 165)
        txtCharDetails.TabIndex = 12
        '
        'lblCharDetails
        '
        lblCharDetails.AutoSize = True
        lblCharDetails.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblCharDetails.Location = New Point(410, 137)
        lblCharDetails.Name = "lblCharDetails"
        lblCharDetails.Size = New Size(125, 15)
        lblCharDetails.TabIndex = 13
        lblCharDetails.Text = "Characteristic Details"
        '
        'txtFilePath
        '
        txtFilePath.Location = New Point(410, 443)
        txtFilePath.Name = "txtFilePath"
        txtFilePath.PlaceholderText = "No file selected"
        txtFilePath.ReadOnly = True
        txtFilePath.Size = New Size(280, 23)
        txtFilePath.TabIndex = 14
        '
        'btnBrowseFile
        '
        btnBrowseFile.Location = New Point(696, 443)
        btnBrowseFile.Name = "btnBrowseFile"
        btnBrowseFile.Size = New Size(94, 23)
        btnBrowseFile.TabIndex = 15
        btnBrowseFile.Text = "Browse..."
        btnBrowseFile.UseVisualStyleBackColor = True
        '
        'lblFile
        '
        lblFile.AutoSize = True
        lblFile.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblFile.Location = New Point(410, 425)
        lblFile.Name = "lblFile"
        lblFile.Size = New Size(28, 15)
        lblFile.TabIndex = 16
        lblFile.Text = "File"
        '
        'btnSendFile
        '
        btnSendFile.Location = New Point(410, 472)
        btnSendFile.Name = "btnSendFile"
        btnSendFile.Size = New Size(380, 35)
        btnSendFile.TabIndex = 17
        btnSendFile.Text = "Send File"
        btnSendFile.UseVisualStyleBackColor = True
        '
        'DeviceServicesForm
        '
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(804, 541)
        Controls.Add(btnSendFile)
        Controls.Add(lblFile)
        Controls.Add(btnBrowseFile)
        Controls.Add(txtFilePath)
        Controls.Add(lblCharDetails)
        Controls.Add(txtCharDetails)
        Controls.Add(lblDataToSend)
        Controls.Add(btnSend)
        Controls.Add(txtData)
        Controls.Add(btnBack)
        Controls.Add(lblCharacteristicsTitle)
        Controls.Add(cmbCharacteristics)
        Controls.Add(btnDisconnect)
        Controls.Add(lblServicesTitle)
        Controls.Add(lstServices)
        Controls.Add(lblConnectionStatus)
        Controls.Add(lblDeviceAddress)
        Controls.Add(lblDeviceName)
        Name = "DeviceServicesForm"
        Text = "BLE Device Services"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents lblDeviceName As Label
    Friend WithEvents lblDeviceAddress As Label
    Friend WithEvents lblConnectionStatus As Label
    Friend WithEvents lstServices As ListBox
    Friend WithEvents lblServicesTitle As Label
    Friend WithEvents btnDisconnect As Button
    Friend WithEvents cmbCharacteristics As ComboBox
    Friend WithEvents lblCharacteristicsTitle As Label
    Friend WithEvents btnBack As Button
    Friend WithEvents txtData As TextBox
    Friend WithEvents btnSend As Button
    Friend WithEvents lblDataToSend As Label
    Friend WithEvents txtCharDetails As TextBox
    Friend WithEvents lblCharDetails As Label
    Friend WithEvents txtFilePath As TextBox
    Friend WithEvents btnBrowseFile As Button
    Friend WithEvents lblFile As Label
    Friend WithEvents btnSendFile As Button

End Class
