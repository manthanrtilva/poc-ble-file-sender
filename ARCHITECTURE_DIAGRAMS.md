# Visual Architecture & Message Format Diagram

## 🏗️ Code Architecture After Refactoring

```
┌─────────────────────────────────────────────────────────────────┐
│                    DeviceServicesForm.vb                         │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  btnSendFile_Click() - Main File Transfer Coordinator   │   │
│  │                                                          │   │
│  │  1. Read file & calculate CRC32                         │   │
│  │  2. Build metadata → BuildMetadataPacket()              │   │
│  │  3. Find BLE characteristics                            │   │
│  │  4. Send metadata → SendDataToCharacteristic()          │   │
│  │  5. Send data → SendFileDataInChunks()                  │   │
│  │  6. Display result                                       │   │
│  └─────────────────────────────────────────────────────────┘   │
│         │                    │                      │           │
│         ▼                    ▼                      ▼           │
│  ┌────────────────┐  ┌──────────────────┐  ┌──────────────┐   │
│  │BuildMetadata   │  │SendDataToChar    │  │SendFileData  │   │
│  │Packet()        │  │acteristic()      │  │InChunks()    │   │
│  │                │  │                  │  │              │   │
│  │Format:         │  │Handles:          │  │Handles:      │   │
│  │[0x01][size]    │  │- DataWriter      │  │- Chunking    │   │
│  │[crc32]         │  │- Write mode sel. │  │- Opcode      │   │
│  │Returns: Byte() │  │- Send via BLE    │  │- Progress    │   │
│  │                │  │Returns: Status   │  │- Delays      │   │
│  │Easy to modify  │  │                  │  │Returns: Int  │   │
│  │for new format  │  │Reusable helper   │  │Easy to modify│   │
│  └────────────────┘  └──────────────────┘  └──────────────┘   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
         │                    │                      │
         ▼                    ▼                      ▼
      ┌──────────┐        ┌──────────┐         ┌──────────┐
      │Metadata  │        │Response  │         │File Data │
      │Char.     │        │(optional)│         │Char.     │
      │11110004  │        │11110004  │         │11110005  │
      └──────────┘        └──────────┘         └──────────┘
         │                    │                      │
         └────────────────────┼──────────────────────┘
                              ▼
                    ┌──────────────────┐
                    │  BLE Device      │
                    │  (N32WB03x)      │
                    │                  │
                    │ - Receive metadata│
                    │ - Verify CRC32   │
                    │ - Receive chunks │
                    │ - Store to flash │
                    │ - Send response  │
                    └──────────────────┘
```

---

## 📦 Metadata Packet Format

### Current (N32WB03x)

```
┌────────────────────────────────────────────────────┐
│  METADATA PACKET (9 bytes total)                   │
├────────────────────────────────────────────────────┤
│ Byte 0:   Opcode = 0x01                            │
│ ─────────────────────────────────────────────────  │
│ Bytes 1-4: File Size (uint32, Little-Endian)       │
│           Byte 1: LSB (bits 0-7)                   │
│           Byte 2: bits 8-15                        │
│           Byte 3: bits 16-23                       │
│           Byte 4: MSB (bits 24-31)                 │
│ ─────────────────────────────────────────────────  │
│ Bytes 5-8: CRC32 (uint32, Little-Endian)           │
│           Byte 5: LSB (bits 0-7)                   │
│           Byte 6: bits 8-15                        │
│           Byte 7: bits 16-23                       │
│           Byte 8: MSB (bits 24-31)                 │
└────────────────────────────────────────────────────┘

Example: File = 100 bytes, CRC32 = 0x12345678
┌─┬─┬─┬─┬─┬─┬─┬─┬─┐
│1│64│0│0│0│78│56│34│12│
├─┴─┴─┴─┴─┴─┴─┴─┴─┤
 Hex bytes sent   
 over BLE

Breakdown:
01        → Opcode (metadata)
64 00 00 00 → Size 100 (0x00000064 in LE)
78 56 34 12 → CRC32 0x12345678 in LE
```

---

## 📦 Data Packet Format

### Current (N32WB03x)

```
┌──────────────────────────────────────────┐
│  DATA PACKET (up to 247 bytes)           │
├──────────────────────────────────────────┤
│ Byte 0:  Opcode = 0x02                   │
│ ──────────────────────────────────────── │
│ Bytes 1+: File data (up to 246 bytes)    │
│          Variable length per BLE write   │
└──────────────────────────────────────────┘

Example: First chunk of file
┌─┬──────────────────────────────────────────┐
│2│[First 20 bytes of file content]          │
├─┴──────────────────────────────────────────┤
 Opcode  Raw file data (20 bytes this time)

Chunks sent repeatedly until entire file
is transferred to device.
```

---

## 🔄 Communication Sequence

```
CLIENT                          DEVICE
 │                              │
 │ 1. Read file                 │
 │    Calculate CRC32           │
 │    Build metadata packet     │
 │                              │
 │ 2. Send metadata ────────────►│
 │    [0x01][size][crc]         │ Receive & store metadata
 │                              │ Prepare flash buffer
 │                              │
 │ 3. [Optional] Read response ◄┤ Send status (if readable)
 │    Verify acceptance         │
 │                              │
 │ 4. Wait 100ms ────────────────│ Process metadata
 │                              │
 │ 5. Send chunk 1 ──────────────►│ Receive & buffer
 │    [0x02][20 bytes]          │ 
 │                              │
 │    Send chunk 2 ──────────────►│ Receive & buffer
 │    [0x02][20 bytes]          │ Flush when buffer full
 │                              │
 │    Send chunk 3 ──────────────►│ Receive & buffer
 │    [0x02][20 bytes]          │
 │                              │
 │    ... more chunks ...       │ ... more receives ...
 │                              │
 │    Send final chunk ──────────►│ Receive & buffer
 │    [0x02][N bytes]           │ Flush remaining
 │                              │ Calculate CRC32
 │                              │ Verify match
 │                              │
 │ 6. Wait 100ms ────────────────│ Verify complete
 │                              │
 │ 7. [Optional] Read final ◄────│ Send final status
 │    response                  │ (0x00 = success)
 │                              │
 │ 8. Display success message   │
 │                              │
```

---

## 🔀 Function Call Flow

```
User clicks "Send File"
         │
         ▼
┌─────────────────────────────────────────┐
│ btnSendFile_Click()                     │
│                                         │
│ 1. Validate file exists                 │
│ 2. Read all bytes into fileBytes[]      │
│ 3. Pad to 4-byte boundary               │
│ 4. Calculate CRC32                      │
│    └─► CalculateCRC32(paddedBytes)      │
│ 5. Build metadata                       │
│    └─► BuildMetadataPacket(size, crc)   │
│         Returns: Byte[9]                │
│ 6. Find characteristics                 │
│ 7. Send metadata                        │
│    └─► SendDataToCharacteristic(        │
│          metadataChar,                  │
│          metadataBytes)                 │
│         Returns: GattStatus             │
│ 8. Read response [optional]             │
│ 9. Send file data                       │
│    └─► SendFileDataInChunks(            │
│          fileDataChar,                  │
│          fileBytes)                     │
│         Returns: Int (bytes sent)       │
│ 10. Read response [optional]            │
│ 11. Display success message             │
└─────────────────────────────────────────┘
         │
         ▼
   Transfer Complete
```

---

## 🛠️ Function Signature Reference

```
BuildMetadataPacket
┌─────────────────────────────────────────────────┐
│ Input:  fileSize (UInteger)                     │
│         crc32Value (UInteger)                   │
│ Process: Pack into 9-byte array with opcode    │
│ Output:  Byte[9]                               │
│ Usage:   metadataBytes = BuildMetadataPacket(...) │
└─────────────────────────────────────────────────┘

SendDataToCharacteristic
┌─────────────────────────────────────────────────┐
│ Input:  characteristic (GattCharacteristic)    │
│         data (Byte[])                          │
│ Process: Create DataWriter, detect write mode,│
│          send via BLE                         │
│ Output:  Task(Of GattCommunicationStatus)      │
│ Usage:   status = Await SendDataToChar(...)    │
└─────────────────────────────────────────────────┘

SendFileDataInChunks
┌─────────────────────────────────────────────────┐
│ Input:  fileDataChar (GattCharacteristic)      │
│         fileBytes (Byte[])                     │
│ Process: Split into 20-byte chunks,           │
│          add 0x02 opcode,                     │
│          send all chunks                      │
│ Output:  Task(Of Integer) - bytes sent        │
│ Usage:   sent = Await SendFileDataInChunks(...) │
└─────────────────────────────────────────────────┘
```

---

## 📊 Byte Layout Diagrams

### Metadata Packet (Detailed)

```
Byte Offset:  0    1    2    3    4    5    6    7    8
┌────────────────────────────────────────────────────────┐
│ 0x01│LSB  │    │    │MSB  │LSB  │    │    │MSB  │
├────┼─────┼────┼────┼─────┼─────┼────┼────┼─────┤
│Op  │      SIZE (4 bytes, LE)       │      CRC32 (4 bytes, LE)    │
└────┴─────┴────┴────┴─────┴─────┴────┴────┴─────┘

Bit positions (size field):
Byte 1: bits 0-7   (LSB of size)
Byte 2: bits 8-15  
Byte 3: bits 16-23 
Byte 4: bits 24-31 (MSB of size)

Example: Size = 0x00000050 (80 decimal)
50 00 00 00 = 0x50 | 0x00 | 0x00 | 0x00
```

### Data Packet (Detailed)

```
Byte Offset:  0    1    2    3    4   ...
┌─────────────────────────────────────────┐
│ 0x02│[Data continues up to 246 bytes]   │
├─────┼────────────────────────────────────┤
│OP  │        RAW FILE BYTES (variable)   │
└─────┴────────────────────────────────────┘

Variable length per BLE MTU:
- Minimum: 1 byte opcode + 1 byte data = 2 bytes
- Typical: 1 byte opcode + 20 bytes data = 21 bytes
- Maximum: 1 byte opcode + 246 bytes data = 247 bytes
```

---

## 🔄 Bit Order Examples

### Little-Endian (Current)
```
Value: 0x12345678 (4-byte integer)
Bits:  [31:24][23:16][15:8][7:0]
       0x12    0x34   0x56 0x78

In memory (Little-Endian):
Byte 0: 0x78 ◄─ LSB (least significant)
Byte 1: 0x56
Byte 2: 0x34
Byte 3: 0x12 ◄─ MSB (most significant)

This is what Intel x86 uses.
```

### Big-Endian (If Hardware Changes)
```
Value: 0x12345678 (4-byte integer)

In memory (Big-Endian):
Byte 0: 0x12 ◄─ MSB (most significant)
Byte 1: 0x34
Byte 2: 0x56
Byte 3: 0x78 ◄─ LSB (least significant)

This is what network protocols use (TCP/IP).
```

---

## 📈 CRC32 Calculation Flow

```
Input: File content (padded to 4-byte boundary)
│
├─ Initialize CRC = 0xFFFFFFFF
│
├─ For each byte in padded file:
│  ├─ Calculate index = (CRC >> 24) ^ byte
│  ├─ CRC = (CRC << 8) ^ crcTable[index]
│
└─ Output: Final CRC value (no final XOR)

Polynomial: 0x4C11DB7 (MPEG-2 standard)
This matches N32WB03x hardware CRC unit.
```

---

## 🔌 Characteristic UUIDs

```
Metadata Characteristic
├─ UUID: 11110004-1111-1111-1111-111111111111
├─ Properties: Write, Notify (optional)
├─ Max Size: 247 bytes (BLE ATT limit)
└─ Purpose: Send [0x01][size][crc]

Data Characteristic
├─ UUID: 11110005-1111-1111-1111-111111111111
├─ Properties: Write, Write Without Response
├─ Max Size: 247 bytes (BLE ATT limit)
└─ Purpose: Send [0x02][file bytes...]
```

---

## 📱 Hardware Reference

```
Device: Nations N32WB03x
├─ Processor: ARM Cortex-M0
├─ Flash: 128-256 KB
├─ RAM: 16-32 KB
├─ BLE: Version 4.2
├─ MTU: 247 bytes (20-byte default)
└─ CRC Unit: MPEG-2, non-reflected

Pattern Storage
├─ Start Address: 0x00000000 (example, verify)
├─ Size: See NS_IMAGE_PATTERN_SIZE
└─ Sectors: Erased before write

CRC Hardware
├─ Polynomial: 0x4C11DB7
├─ Init: 0xFFFFFFFF
├─ Input Mode: Non-reflected byte
└─ Output Mode: Non-reflected (32-bit)
```

---

## 🔍 Debugging Checklist (Visual)

```
File Transfer Started
         │
         ▼
[Metadata Bytes Correct?]
├─ Yes ──► [Device Accepts?]
│          ├─ Yes ──► [Data Sent?]
│          │          ├─ Yes ──► [CRC Matches?]
│          │          │          ├─ Yes ──► ✅ SUCCESS
│          │          │          └─ No ──► ❌ CRC Error
│          │          └─ No ──► ❌ Data Not Received
│          └─ No ──► ❌ Metadata Rejected
└─ No ──► ❌ Format Error (byte order? field order?)
```

---

## 📊 Message Format Comparison Table

| Aspect | Current | Big-Endian | Field Reorder |
|--------|---------|-----------|---------------|
| Opcode | 0x01 | 0x01 | 0x01 |
| Size | LE (bytes 1-4) | BE (bytes 1-4) | Byte 5-8 |
| CRC | LE (bytes 5-8) | BE (bytes 5-8) | Byte 1-4 |
| Total Length | 9 | 9 | 9 |
| BuildMetadata | Set `metadataBytes(1-4)` shift 0,8,16,24 | Set `metadataBytes(1-4)` shift 24,16,8,0 | Set `metadataBytes(5-8)` for size, `(1-4)` for CRC |

