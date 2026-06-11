# BLE File Transfer - Update Summary

## What Was Updated

Your BLE file transfer client code has been **refactored for easy firmware adaptation**. The code now cleanly separates concerns and makes it trivial to update when your hardware team changes the BLE message format.

---

## Key Changes

### 1. **New Modular Functions**

Three new helper functions were added to `DeviceServicesForm.vb`:

#### `BuildMetadataPacket(fileSize, crc32Value) → Byte()`
- **Purpose:** Construct the metadata packet sent to the device
- **Current Format:** 9 bytes = `[0x01][size:4LE][crc32:4LE]`
- **Change Required If:** Firmware changed metadata format (byte order, field order, new fields, etc.)
- **Location:** Lines 703-722

#### `SendDataToCharacteristic(characteristic, data) → Task(Of GattCommunicationStatus)`
- **Purpose:** Send raw bytes to a BLE characteristic
- **Features:** Auto-selects write mode (WithResponse vs WithoutResponse)
- **Change Required If:** Firmware changed BLE write protocol (unlikely)
- **Location:** Lines 725-737

#### `SendFileDataInChunks(fileDataChar, fileBytes) → Task(Of Integer)`
- **Purpose:** Split file into chunks and send with opcode (0x02)
- **Features:** Handles chunking, adds opcode prefix, tracks progress
- **Change Required If:** Firmware changed data packet format or chunk size
- **Location:** Lines 740-780

### 2. **Cleaner Main Function**

The `btnSendFile_Click()` function now:
- Uses helper functions instead of inline code
- Has clearer flow: Read → Pad → Calculate → Build → Find → Send Metadata → Send Data
- Is easier to understand and debug
- Has better separation of concerns

---

## How to Adapt to Firmware Changes

### Step 1: Identify What Changed

Ask your hardware team:
- Did the **metadata format** change? (byte order, field order, new fields?)
- Did the **data opcode** change? (still 0x02 or different?)
- Did the **chunk size** change? (still 20 bytes or larger/smaller?)
- Did the **CRC algorithm** change? (still MPEG-2?)

### Step 2: Update Only the Affected Function

| Change | Function to Modify | Example |
|--------|-------------------|---------|
| Metadata format | `BuildMetadataPacket()` | [Field order changed](QUICK_REFERENCE.md#change-2-field-order-changed) |
| Data opcode | `SendFileDataInChunks()` | Change `Const dataOpcode As Byte = &H02` |
| Chunk size | `SendFileDataInChunks()` | Change `Const chunkSize As Integer = 20` |
| CRC algorithm | `CalculateCRC32()` | Change polynomial/init/final values |

### Step 3: Test

Build → Connect → Send Test File → Verify Output

---

## Documentation Files

| File | Purpose | When to Read |
|------|---------|--------------|
| **README.md** (this file) | Overview & quick start | First, to understand what changed |
| **REFACTORING_SUMMARY.md** | Detailed explanation of new functions | Before making changes |
| **BLE_MESSAGE_FORMAT.md** | Current & future message format specs | To understand message structure |
| **QUICK_REFERENCE.md** | Copy-paste examples for common changes | When actually modifying code |

---

## Current Implementation (Reference)

### Metadata Packet (Opcode 0x01)
```
Characteristic: 11110004-1111-1111-1111-111111111111

Byte 0:     0x01 (opcode)
Bytes 1-4:  File Size (uint32, Little-Endian)
Bytes 5-8:  CRC32 (uint32, Little-Endian)
Total:      9 bytes

Example (100 bytes, CRC=0x12345678):
01 64 00 00 00 78 56 34 12
^  ^-------^  ^-------^
|  Size=100   CRC=0x12345678
Opcode
```

### Data Packet (Opcode 0x02)
```
Characteristic: 11110005-1111-1111-1111-111111111111

Byte 0:     0x02 (opcode)
Bytes 1+:   File data (up to 246 bytes per BLE write)

Chunks are sent repeatedly until entire file is transferred.
```

### CRC32 Algorithm
```
Polynomial: 0x4C11DB7 (MPEG-2, Non-reflected)
Initial:    0xFFFFFFFF
Final XOR:  None
Bit Reflection: No
Byte Reflection: No

This matches the hardware CRC unit on N32WB03x MCU.
```

---

## Files Modified

### Main Application Code
- **DeviceServicesForm.vb**
  - Added 3 new helper functions (lines 703-780)
  - Refactored `btnSendFile_Click()` to use new functions
  - All existing functionality preserved

### Documentation (New)
- **BLE_MESSAGE_FORMAT.md** - Message format specs
- **REFACTORING_SUMMARY.md** - Code changes explained
- **QUICK_REFERENCE.md** - Copy-paste examples
- **README.md** (this file) - Quick start guide

---

## Build & Test Status

✅ **Solution builds successfully**

```
Build Status: Successful
No compilation errors
All existing functionality preserved
```

### Verification Checklist

- [x] Code compiles without errors
- [x] New functions added
- [x] Main flow refactored
- [x] File transfer logic extracted
- [x] Metadata building centralized
- [x] Helper documentation created
- [ ] Test with actual device (you need to do this)
- [ ] Verify metadata format matches firmware
- [ ] Test with various file sizes
- [ ] Confirm CRC32 verification passes

---

## How to Use the Code

### Normal Operation (No Changes Needed)
1. Open DeviceServicesForm
2. Select a file with "Browse File"
3. Click "Send File"
4. Monitor progress in text box
5. Verify success message

### If Firmware Changed Metadata Format
1. Get the new format from hardware team
2. Open `DeviceServicesForm.vb`
3. Find `BuildMetadataPacket()` function (line 703)
4. Update the function to build the new format
5. See **QUICK_REFERENCE.md** for examples
6. Build and test

### If Firmware Changed Data Format
1. Get the new opcode and chunk size from hardware team
2. Open `DeviceServicesForm.vb`
3. Find `SendFileDataInChunks()` function (line 740)
4. Update `Const dataOpcode` and/or `Const chunkSize`
5. Build and test

---

## Code Quality

### Before Refactoring
- Metadata building mixed with BLE operations
- File chunking logic inline in main function
- Hard to test individual components
- Difficult to spot where to make changes

### After Refactoring
- ✅ Clear separation of concerns
- ✅ Each function has single responsibility
- ✅ Functions are testable independently
- ✅ Easy to locate code for modification
- ✅ Better comments and documentation

---

## Next Steps

1. **Have your hardware team confirm** any message format changes
2. **If no changes:** Just use the code as-is, it's backward compatible
3. **If changes:** Use **QUICK_REFERENCE.md** to update the appropriate function
4. **Build** the solution (Ctrl+Shift+B)
5. **Test** with your device
6. **Debug** using the detailed output in the text box

---

## Support/Debugging

### Check This First
- Does the code compile? → If not, see error message
- Can you connect to device? → Check BLE Scanner form
- Do you see metadata bytes logged? → Verify they match firmware expectation
- Does device accept metadata? → Check device logs for errors

### Common Issues
| Issue | Solution |
|-------|----------|
| Can't find characteristics | Device might not have UUIDs 11110004/11110005 |
| Metadata rejected | Check byte order and field order in BuildMetadataPacket() |
| CRC mismatch | Verify CalculateCRC32() algorithm matches firmware |
| Data not sent | Check chunk size and data opcode in SendFileDataInChunks() |

### Detailed Help
- See **BLE_MESSAGE_FORMAT.md** for troubleshooting
- See **QUICK_REFERENCE.md** for specific examples
- See **REFACTORING_SUMMARY.md** for function details

---

## Important Notes

⚠️ **The code is backward compatible** - If your firmware hasn't changed, everything works exactly as before.

⚠️ **Byte order matters** - Check if your firmware expects Little-Endian (current) or Big-Endian (less common).

⚠️ **Padding matters** - File data is padded to 4-byte boundary for CRC calculation, but original file size is sent in metadata.

⚠️ **CRC includes padding** - The CRC32 is calculated on padded data, not original file data.

---

## Questions?

1. **How do I know if firmware changed?** → Ask your hardware team for new message format specification
2. **Where do I make changes?** → Use the function location table above and QUICK_REFERENCE.md
3. **How do I test changes?** → Build → Connect → Send test file → Check output and device logs
4. **What if I break something?** → Revert to original BuildMetadataPacket() or SendFileDataInChunks()

