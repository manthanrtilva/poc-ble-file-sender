<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
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
        btnScan = New Button()
        lstDevices = New ListBox()
        lblStatus = New Label()
        txtSearch = New TextBox()
        SuspendLayout()
        ' 
        ' btnScan
        ' 
        btnScan.Location = New Point(12, 12)
        btnScan.Name = "btnScan"
        btnScan.Size = New Size(150, 40)
        btnScan.TabIndex = 0
        btnScan.Text = "Scan BLE Devices"
        btnScan.UseVisualStyleBackColor = True
        ' 
        ' lstDevices
        ' 
        lstDevices.FormattingEnabled = True
        lstDevices.ItemHeight = 15
        lstDevices.Location = New Point(12, 58)
        lstDevices.Name = "lstDevices"
        lstDevices.Size = New Size(776, 349)
        lstDevices.TabIndex = 1
        ' 
        ' lblStatus
        ' 
        lblStatus.AutoSize = True
        lblStatus.Location = New Point(180, 22)
        lblStatus.Name = "lblStatus"
        lblStatus.Size = New Size(0, 15)
        lblStatus.TabIndex = 2
        ' 
        ' txtSearch
        ' 
        txtSearch.Location = New Point(180, 20)
        txtSearch.Name = "txtSearch"
        txtSearch.PlaceholderText = "Search devices..."
        txtSearch.Size = New Size(300, 23)
        txtSearch.TabIndex = 3
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(txtSearch)
        Controls.Add(lblStatus)
        Controls.Add(lstDevices)
        Controls.Add(btnScan)
        Name = "Form1"
        Text = "BLE File Sender"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents btnScan As Button
    Friend WithEvents lstDevices As ListBox
    Friend WithEvents lblStatus As Label
    Friend WithEvents txtSearch As TextBox

End Class
