# BLE Message Format Documentation

## Overview
This document describes how the BLE file transfer protocol works between the .NET client and the N32WB03x firmware device.

## Current Implementation

### Metadata Packet (Opcode 0x01)
**Characteristic UUID:** `11110004-1111-1111-1111-111111111111`

**Format:**
```
Byte 0:     Opcode = 0x01
Bytes 1-4:  File size (32-bit unsigned integer, Little-Endian)
Bytes 5-8:  CRC32 checksum (32-bit unsigned integer, Little-Endian)
```

**Total:** 9 bytes

**Example:**
```
01 50 00 00 00 A1 B2 C3 D4
^  ^--------^  ^--------^
|  |          |
|  |          CRC32: 0xD4C3B2A1
|  File Size: 0x00000050 (80 bytes)
Opcode
```

### Data Packet (Opcode 0x02)
**Characteristic UUID:** `11110005-1111-1111-1111-111111111111`

**Format:**
```
Byte 0:      Opcode = 0x02
Bytes 1+:    File data (up to 246 bytes per BLE write)
```

**Note:** Data is sent in chunks, each prefixed with the 0x02 opcode.

---

## How to Update for Firmware Changes

### Step 1: Identify the New Metadata Format

Contact your hardware team to get the exact new metadata format. For example, they might have changed it to:
- Different byte order (Big-Endian instead of Little-Endian)
- Different field order or size
- Additional fields (e.g., version, flags, timestamp)
- Different opcode values

Example of a potential new format:
```
Byte 0:     Opcode = 0x01
Bytes 1-4:  CRC32 checksum (Big-Endian, moved before size)
Bytes 5-8:  File size (Big-Endian)
Byte 9:     Version/Flags
```

### Step 2: Update BuildMetadataPacket() Function

Locate this function in `DeviceServicesForm.vb`:

```vb
Private Function BuildMetadataPacket(fileSize As UInteger, crc32Value As UInteger) As Byte()
```

**Current Implementation:**
```vb
Private Function BuildMetadataPacket(fileSize As UInteger, crc32Value As UInteger) As Byte()
    Dim metadataBytes(8) As Byte ' 9 bytes total
    metadataBytes(0) = &H01 ' Opcode for metadata

    ' File size in little-endian (4 bytes)
    metadataBytes(1) = CByte((fileSize >> 0) And &HFF)
    metadataBytes(2) = CByte((fileSize >> 8) And &HFF)
    metadataBytes(3) = CByte((fileSize >> 16) And &HFF)
    metadataBytes(4) = CByte((fileSize >> 24) And &HFF)

    ' CRC32 in little-endian (4 bytes)
    metadataBytes(5) = CByte((crc32Value >> 0) And &HFF)
    metadataBytes(6) = CByte((crc32Value >> 8) And &HFF)
    metadataBytes(7) = CByte((crc32Value >> 16) And &HFF)
    metadataBytes(8) = CByte((crc32Value >> 24) And &HFF)

    Return metadataBytes
End Function
```

**Example: If firmware changed to Big-Endian with CRC first:**
```vb
Private Function BuildMetadataPacket(fileSize As UInteger, crc32Value As UInteger) As Byte()
    Dim metadataBytes(8) As Byte ' 9 bytes total
    metadataBytes(0) = &H01 ' Opcode for metadata

    ' CRC32 in big-endian (4 bytes)
    metadataBytes(1) = CByte((crc32Value >> 24) And &HFF)
    metadataBytes(2) = CByte((crc32Value >> 16) And &HFF)
    metadataBytes(3) = CByte((crc32Value >> 8) And &HFF)
    metadataBytes(4) = CByte((crc32Value >> 0) And &HFF)

    ' File size in big-endian (4 bytes)
    metadataBytes(5) = CByte((fileSize >> 24) And &HFF)
    metadataBytes(6) = CByte((fileSize >> 16) And &HFF)
    metadataBytes(7) = CByte((fileSize >> 8) And &HFF)
    metadataBytes(8) = CByte((fileSize >> 0) And &HFF)

    Return metadataBytes
End Function
```

### Step 3: Update SendFileDataInChunks() If Needed

If the data packet format changed (e.g., opcode value or data encoding), update this function:

```vb
Private Async Function SendFileDataInChunks(fileDataChar As GattCharacteristic, fileBytes As Byte()) As Task(Of Integer)
```

**Current Implementation uses:**
- Opcode: 0x02 (stored in constant `dataOpcode`)
- Simple binary data

**If firmware changed the opcode:**
```vb
Const dataOpcode As Byte = &H03 ' Changed from 0x02 to 0x03 (example)
```

**If firmware requires encoding (e.g., Base64):**
```vb
' Add encoding before building packet:
Dim encodedData As String = Convert.ToBase64String(fileBytes.Skip(offset).Take(currentChunkSize).ToArray())
Dim encodedBytes As Byte() = System.Text.Encoding.ASCII.GetBytes(encodedData)
' Then use encodedBytes instead of raw file data
```

### Step 4: Test the Changes

1. **Build the solution** to ensure no compilation errors
2. **Connect to your device** via the BLE Scanner form
3. **Select a test file** (start with a small file, e.g., 100 bytes)
4. **Click "Send File"** and monitor the output in the text box
5. **Check the device logs** to verify it received the correct metadata and data

---

## CRC32 Calculation Notes

The current implementation uses **MPEG-2 CRC32** (Non-reflected):
- **Polynomial:** 0x4C11DB7
- **Initial Value:** 0xFFFFFFFF
- **Final XOR:** None (returns raw CRC value)
- **Bit Reflection:** No
- **Byte Reflection:** No

This matches the hardware CRC unit on the N32WB03x MCU. If your firmware changed the CRC algorithm:

```vb
Private Function CalculateCRC32(data As Byte()) As UInteger
    ' Modify the polynomial or algorithm here if needed
    Const polynomial As UInteger = &H4C11DB7UI ' Change if different
    ' ... rest of implementation
End Function
```

---

## Firmware-Side Implementation Reference

From `app_ns_ius.c`:
```c
case PATTERN_OP_META: {
    // [0x01][size:4 LE][crc32:4 LE]
    if (p_param->length != 9) {
        ns_ble_ius_pattern_status_send(PATTERN_OP_META, PATTERN_STATUS_BAD_PARAM);
        break;
    }
    uint32_t size = p_param->value[1] | p_param->value[2] << 8 |
                    p_param->value[3] << 16 | p_param->value[4] << 24;
    uint32_t crc = p_param->value[5] | p_param->value[6] << 8 |
                   p_param->value[7] << 16 | p_param->value[8] << 24;
    // ...
}
```

---

## Troubleshooting

### "Metadata Characteristic Not Found"
- Verify the firmware has the characteristic with UUID `11110004-1111-1111-1111-111111111111`
- Check if the UUID needs to be updated in the code (look for `11110004` in `DeviceServicesForm.vb`)

### Device Doesn't Accept Metadata
- Check the metadata format matches exactly (byte order, field sizes, opcode)
- Verify the packet length in the firmware error checking matches your new format

### CRC32 Mismatch
- Ensure the CRC calculation algorithm matches the firmware
- Verify padding is applied correctly (should be to 4-byte boundary)
- Check if firmware changed the CRC polynomial

---

## Files to Modify

1. **DeviceServicesForm.vb**
   - `BuildMetadataPacket()` - Metadata format
   - `SendFileDataInChunks()` - Data packet format
   - `CalculateCRC32()` - CRC algorithm (if needed)
   - Characteristic UUID constants (if changed)

2. **Firmware (Reference)**
   - `C:\work\zuru\lightstrip\src\app_profile\app_ns_ius.c` - Message handler
   - `C:\work\zuru\lightstrip\src\app_flash_pattern.c` - CRC verification

