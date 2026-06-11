# 🎯 Quick Reference Card

**Print this page for quick access!**

---

## 📋 One-Page Cheat Sheet

### 1️⃣ Build Status
```
✅ SUCCESSFUL - Ready to use
❌ Errors: 0
⚠️  Warnings: 0
```

### 2️⃣ What Changed
```
Modified: DeviceServicesForm.vb
Added:    BuildMetadataPacket()
Added:    SendDataToCharacteristic()
Added:    SendFileDataInChunks()
```

### 3️⃣ Current Message Format
```
Metadata: [0x01][size:4LE][crc32:4LE] = 9 bytes
Data:     [0x02][file data...] = up to 247 bytes
CRC:      MPEG-2, polynomial 0x4C11DB7
```

### 4️⃣ Your Next Steps
```
□ Read README.md (5 min)
□ Test with device
□ Ask: "Did firmware change?"
  ├─ No → Done!
  └─ Yes → Use QUICK_REFERENCE.md
```

### 5️⃣ If Firmware Changed

**Metadata Format?**
→ Edit `BuildMetadataPacket()` function
→ See QUICK_REFERENCE.md - Changes 1, 2, 3

**Data Opcode?**
→ Edit `SendFileDataInChunks()` - Change opcode constant
→ See QUICK_REFERENCE.md - Change 4

**Chunk Size?**
→ Edit `SendFileDataInChunks()` - Change size constant
→ See QUICK_REFERENCE.md - Change 6

**CRC Algorithm?**
→ Edit `CalculateCRC32()` function
→ See QUICK_REFERENCE.md - Change 7

---

## 🚀 Build & Run

```
Ctrl+Shift+B = Build solution
F5           = Run application
Shift+F5     = Stop debugging
Ctrl+F       = Find in files
```

---

## 🎯 File Locations

```
Main Code:     DeviceServicesForm.vb
├─ BuildMetadataPacket()         Line 703
├─ SendDataToCharacteristic()     Line 725
└─ SendFileDataInChunks()         Line 740

Firmware:
├─ app_ns_ius.c                  Message handler
├─ app_flash_pattern.c            CRC verification
└─ n32wb03x.h                    Hardware definitions
```

---

## 📊 Byte Order Reference

### Little-Endian (Current)
```
0x12345678 →  78 56 34 12
            LSB          MSB
```

### Big-Endian (If Changes)
```
0x12345678 →  12 34 56 78
            MSB          LSB
```

---

## 🔧 Most Common Changes

| Change | Where | How | Time |
|--------|-------|-----|------|
| Byte order | BuildMetadataPacket() | Swap shift amounts | 5 min |
| Field order | BuildMetadataPacket() | Reorder array | 5 min |
| Opcode | SendFileDataInChunks() | Change constant | 2 min |
| Chunk size | SendFileDataInChunks() | Change constant | 2 min |

---

## 📞 Characteristic UUIDs

```
Metadata:  11110004-1111-1111-1111-111111111111
Data:      11110005-1111-1111-1111-111111111111
```

If changed, find and replace these GUIDs in:
- Line 398: `Dim metadataCharUuid As New Guid(...)`
- Line 399: `Dim fileDataCharUuid As New Guid(...)`

---

## 🐛 Quick Troubleshooting

### Device rejects metadata
```
□ Check metadata bytes in output text box
□ Compare with firmware specification
□ Verify byte order (LE vs BE)
□ Verify field order
```

### CRC32 doesn't match
```
□ Verify CRC algorithm matches
□ Check padding (4-byte boundary)
□ Check byte order in metadata
```

### Data not received
```
□ Check data opcode (0x02?)
□ Verify chunk size
□ Check BLE write succeeded
```

---

## 📚 Documentation Quick Links

| Need | File | Section |
|------|------|---------|
| Overview | README.md | All |
| Code details | CODE_CHANGES.md | New Functions |
| Improvements | BEFORE_AFTER_COMPARISON.md | Summary Table |
| Message specs | BLE_MESSAGE_FORMAT.md | Current Implementation |
| Examples | QUICK_REFERENCE.md | All 7 Changes |
| Testing | CHECKLIST.md | Your Next Steps |
| Diagrams | ARCHITECTURE_DIAGRAMS.md | All |
| Navigation | INDEX.md | Learning Path |

---

## ✅ Testing Checklist

```
□ Build (Ctrl+Shift+B)
□ Connect to device
□ Send 100-byte file
  └─ Check output text box for metadata bytes
□ Verify device receives
□ Send 500-byte file
  └─ Check chunking works
□ Check CRC32 matches
□ If large files: test 2KB+ file
```

---

## 🎓 Learning Paths

**5-minute version:**
```
README.md
```

**15-minute version:**
```
README.md
CODE_CHANGES.md
```

**30-minute version:**
```
README.md
CODE_CHANGES.md
QUICK_REFERENCE.md
```

**1-hour version:**
```
All documentation files
```

---

## 💾 Keyboard Shortcuts

```
Ctrl+S      = Save
Ctrl+Shift+S = Save All
Ctrl+Z      = Undo
Ctrl+Y      = Redo
Ctrl+F      = Find
Ctrl+H      = Replace
Ctrl+Shift+F = Search all files
F5          = Debug Run
Shift+F5    = Stop Debug
Ctrl+Shift+B = Build Solution
```

---

## 🔑 Key Constants

In `SendFileDataInChunks()`:
```vb
Const chunkSize As Integer = 20      ' Change if needed
Const dataOpcode As Byte = &H02       ' Change if needed
```

In `BuildMetadataPacket()`:
```vb
metadataBytes(0) = &H01              ' Metadata opcode
' Change byte order here if firmware requires
```

---

## 📱 Hardware Specs

```
MCU:        N32WB03x
BLE:        4.2
MTU:        247 bytes
CRC Unit:   MPEG-2, non-reflected
Poly:       0x4C11DB7
Init:       0xFFFFFFFF
Final XOR:  None
```

---

## 🎯 Success = When...

```
✅ Code builds without errors
✅ Small file transfers work
✅ CRC32 matches on device
✅ Device logs show success
✅ No BLE timeouts
```

---

## 🆘 Get Help

```
Error message → CODE_CHANGES.md
Format question → BLE_MESSAGE_FORMAT.md
Modification → QUICK_REFERENCE.md
Testing → CHECKLIST.md
Architecture → ARCHITECTURE_DIAGRAMS.md
Navigation → INDEX.md
```

---

## ⏱️ Estimated Times

```
Build:                2 minutes
Test with current FW: 5 minutes
Understand changes:   15 minutes
Adapt to new FW:      10 minutes
```

---

## 🚀 I'm Ready When...

```
✅ I understand what changed (READ: CODE_CHANGES.md)
✅ I know the current format (READ: BLE_MESSAGE_FORMAT.md)
✅ I tested with current firmware (TEST: Send file)
✅ I know what firmware expects (ASK: Hardware team)
✅ I found the right function to modify (USE: Quick ref)
✅ I copied the code example (COPY: QUICK_REFERENCE.md)
✅ I built and tested (BUILD: Ctrl+Shift+B)
```

---

**Bookmark this page for quick access! 📌**

**Questions? See INDEX.md for navigation** 🗺️

