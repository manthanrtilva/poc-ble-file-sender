# Quick Reference - Common Format Changes

## Current Format (N32WB03x)

### Metadata Packet
```
Byte 0:     0x01
Bytes 1-4:  File Size (Little-Endian, uint32)
Bytes 5-8:  CRC32 (Little-Endian, uint32)
Total:      9 bytes
```

**Example (100 bytes, CRC=0x12345678):**
```
01 64 00 00 00 78 56 34 12
```

### Data Packet
```
Byte 0:     0x02
Bytes 1+:   File data (up to 246 bytes)
```

---

## Common Firmware Changes & How to Handle Them

### Change 1: Big-Endian Instead of Little-Endian

**New Format Example:**
```
Byte 0:     0x01
Bytes 1-4:  File Size (Big-Endian)
Bytes 5-8:  CRC32 (Big-Endian)
```

**Update Function:**
```vb
Private Function BuildMetadataPacket(fileSize As UInteger, crc32Value As UInteger) As Byte()
    Dim metadataBytes(8) As Byte
    metadataBytes(0) = &H01

    ' Size in Big-Endian (most significant byte first)
    metadataBytes(1) = CByte((fileSize >> 24) And &HFF)
    metadataBytes(2) = CByte((fileSize >> 16) And &HFF)
    metadataBytes(3) = CByte((fileSize >> 8) And &HFF)
    metadataBytes(4) = CByte((fileSize >> 0) And &HFF)

    ' CRC32 in Big-Endian
    metadataBytes(5) = CByte((crc32Value >> 24) And &HFF)
    metadataBytes(6) = CByte((crc32Value >> 16) And &HFF)
    metadataBytes(7) = CByte((crc32Value >> 8) And &HFF)
    metadataBytes(8) = CByte((crc32Value >> 0) And &HFF)

    Return metadataBytes
End Function
```

**Helper Code for Future Use:**
```vb
' Little-Endian: [LSB, ..., MSB]
' Big-Endian: [MSB, ..., LSB]

' To write a uint32 in any byte order:
Private Function WriteUInt32(value As UInteger, isBigEndian As Boolean) As Byte()
    Dim bytes(3) As Byte

    If isBigEndian Then
        bytes(0) = CByte((value >> 24) And &HFF)
        bytes(1) = CByte((value >> 16) And &HFF)
        bytes(2) = CByte((value >> 8) And &HFF)
        bytes(3) = CByte((value >> 0) And &HFF)
    Else
        bytes(0) = CByte((value >> 0) And &HFF)
        bytes(1) = CByte((value >> 8) And &HFF)
        bytes(2) = CByte((value >> 16) And &HFF)
        bytes(3) = CByte((value >> 24) And &HFF)
    End If

    Return bytes
End Function
```

---

### Change 2: Field Order Changed

**Example: CRC first, then Size**

**New Format:**
```
Byte 0:     0x01
Bytes 1-4:  CRC32 (Little-Endian)
Bytes 5-8:  File Size (Little-Endian)
```

**Update Function:**
```vb
Private Function BuildMetadataPacket(fileSize As UInteger, crc32Value As UInteger) As Byte()
    Dim metadataBytes(8) As Byte
    metadataBytes(0) = &H01

    ' CRC32 FIRST (bytes 1-4)
    metadataBytes(1) = CByte((crc32Value >> 0) And &HFF)
    metadataBytes(2) = CByte((crc32Value >> 8) And &HFF)
    metadataBytes(3) = CByte((crc32Value >> 16) And &HFF)
    metadataBytes(4) = CByte((crc32Value >> 24) And &HFF)

    ' Size SECOND (bytes 5-8)
    metadataBytes(5) = CByte((fileSize >> 0) And &HFF)
    metadataBytes(6) = CByte((fileSize >> 8) And &HFF)
    metadataBytes(7) = CByte((fileSize >> 16) And &HFF)
    metadataBytes(8) = CByte((fileSize >> 24) And &HFF)

    Return metadataBytes
End Function
```

---

### Change 3: Additional Fields (Version, Flags, etc.)

**Example: Added 1-byte version after opcode**

**New Format:**
```
Byte 0:     0x01
Byte 1:     Version (e.g., 0x02)
Bytes 2-5:  File Size (Little-Endian)
Bytes 6-9:  CRC32 (Little-Endian)
Total:      10 bytes
```

**Update Function:**
```vb
Private Function BuildMetadataPacket(fileSize As UInteger, crc32Value As UInteger) As Byte()
    Dim metadataBytes(9) As Byte ' Now 10 bytes instead of 9
    metadataBytes(0) = &H01
    metadataBytes(1) = &H02        ' Version byte

    ' Size (bytes 2-5)
    metadataBytes(2) = CByte((fileSize >> 0) And &HFF)
    metadataBytes(3) = CByte((fileSize >> 8) And &HFF)
    metadataBytes(4) = CByte((fileSize >> 16) And &HFF)
    metadataBytes(5) = CByte((fileSize >> 24) And &HFF)

    ' CRC32 (bytes 6-9)
    metadataBytes(6) = CByte((crc32Value >> 0) And &HFF)
    metadataBytes(7) = CByte((crc32Value >> 8) And &HFF)
    metadataBytes(8) = CByte((crc32Value >> 16) And &HFF)
    metadataBytes(9) = CByte((crc32Value >> 24) And &HFF)

    Return metadataBytes
End Function
```

---

### Change 4: Different Opcode Values

**Old:** Metadata=0x01, Data=0x02
**New:** Metadata=0xAA, Data=0xBB

**Update in SendFileDataInChunks():**
```vb
' Change this line:
Const dataOpcode As Byte = &H02

' To:
Const dataOpcode As Byte = &HBB
```

**Also update BuildMetadataPacket():**
```vb
' Change this line:
metadataBytes(0) = &H01

' To:
metadataBytes(0) = &HAA
```

---

### Change 5: Data Encoding (e.g., Base64)

**Example: Data packets are now Base64-encoded**

**New Format:**
```
Byte 0:     0x02 (opcode)
Bytes 1+:   Base64-encoded file data
```

**Update Function:**
```vb
Private Async Function SendFileDataInChunks(fileDataChar As GattCharacteristic, fileBytes As Byte()) As Task(Of Integer)
    Const chunkSize As Integer = 150  ' Reduce for Base64 expansion
    Const dataOpcode As Byte = &H02

    Dim totalChunks As Integer = Math.Ceiling(fileBytes.Length / chunkSize)
    Dim bytesSent As Integer = 0

    For chunkIndex As Integer = 0 To totalChunks - 1
        Dim offset As Integer = chunkIndex * chunkSize
        Dim currentChunkSize As Integer = Math.Min(chunkSize, fileBytes.Length - offset)

        ' Get raw file chunk
        Dim rawChunk As Byte() = fileBytes.Skip(offset).Take(currentChunkSize).ToArray()

        ' Encode to Base64
        Dim base64String As String = Convert.ToBase64String(rawChunk)
        Dim encodedBytes As Byte() = System.Text.Encoding.ASCII.GetBytes(base64String)

        ' Build packet: [opcode][encoded data]
        Dim packet(encodedBytes.Length) As Byte
        packet(0) = dataOpcode
        Array.Copy(encodedBytes, 0, packet, 1, encodedBytes.Length)

        Dim writeResult = Await SendDataToCharacteristic(fileDataChar, packet)

        If writeResult <> GattCommunicationStatus.Success Then
            Return bytesSent
        End If

        bytesSent += currentChunkSize
        Await Task.Delay(10)

        lblCharacteristicsTitle.Text = $"Sending... {bytesSent}/{fileBytes.Length} bytes ({chunkIndex + 1}/{totalChunks})"
        Application.DoEvents()
    Next

    Return bytesSent
End Function
```

---

### Change 6: Different Chunk Size

**Example: Firmware now expects larger chunks (up to 240 bytes)**

**Update in SendFileDataInChunks():**
```vb
' Change this line:
Const chunkSize As Integer = 20

' To:
Const chunkSize As Integer = 240
```

---

### Change 7: CRC Algorithm Changed

**Example: Firmware now uses CRC-CCITT (0xFFFF init, 0x1021 poly)**

**Update CalculateCRC32():**
```vb
Private Function CalculateCRC32(data As Byte()) As UInteger
    ' Changed from MPEG-2 to CRC-CCITT
    Const polynomial As UInteger = &H1021UI

    ' Build CRC table
    Dim crcTable(255) As UInteger
    For i As UInteger = 0 To 255
        Dim crc As UInteger = i << 8
        For j As Integer = 0 To 7
            If (crc And &H8000UI) <> 0 Then
                crc = (crc << 1) Xor polynomial
            Else
                crc <<= 1
            End If
        Next
        crcTable(i) = crc And &FFFFUI
    Next

    ' Calculate CRC32 (CRC-CCITT: init=0xFFFF, no final XOR)
    Dim crc As UInteger = &FFFFUI
    For Each b As Byte In data
        Dim index As Byte = CByte(((crc >> 8) Xor b) And &HFF)
        crc = ((crc << 8) Xor crcTable(index)) And &FFFFUI
    Next

    Return crc
End Function
```

---

## Testing Your Changes

### Before Testing
1. Save your changes
2. Build the solution (Ctrl+Shift+B)
3. Ensure no compilation errors

### During Testing
1. Connect to device via BLE Scanner
2. Open DevTools (F12) or enable console output
3. Send test file (100-500 bytes)
4. Check output text box for:
   - Metadata Bytes match expected format
   - No communication errors
   - Data sent progress updates
   - Success message

### After Testing
1. Verify device logs show successful reception
2. Check CRC32 matches between client and device
3. Test with larger files if metadata packets worked
4. Try files of various sizes (boundary conditions)

---

## Helper Functions You Can Use

### Write Multi-Byte Integer (Any Endianness)
```vb
Private Function WriteInt32(value As UInteger, isBigEndian As Boolean) As Byte()
    Dim bytes(3) As Byte
    If isBigEndian Then
        Array.Copy(BitConverter.GetBytes(value).Reverse().ToArray(), bytes, 4)
    Else
        Array.Copy(BitConverter.GetBytes(value), bytes, 4)
    End If
    Return bytes
End Function
```

### Read Multi-Byte Integer (Any Endianness)
```vb
Private Function ReadInt32(bytes As Byte(), offset As Integer, isBigEndian As Boolean) As UInteger
    Dim value As UInteger
    If isBigEndian Then
        value = CUInt(bytes(offset) << 24) Or CUInt(bytes(offset + 1) << 16) Or _
                CUInt(bytes(offset + 2) << 8) Or CUInt(bytes(offset + 3))
    Else
        value = CUInt(bytes(offset)) Or CUInt(bytes(offset + 1) << 8) Or _
                CUInt(bytes(offset + 2) << 16) Or CUInt(bytes(offset + 3) << 24)
    End If
    Return value
End Function
```

---

## Common Mistakes to Avoid

| Mistake | Fix |
|---------|-----|
| Byte order reversed | Check bit shift directions (>> vs <<) |
| Off-by-one in array | Remember index 0 is first byte |
| Packet too short/long | Recalculate total packet size |
| Opcode not updated | Search for all hex values in modified functions |
| Forgot to rebuild | Press Ctrl+Shift+B after changes |
| Array initialization wrong | `Dim arr(8) As Byte` creates 9 elements (0-8) |

