# GameInput API alignment with Microsoft documentation

This document describes how **GameInputSharp.Core** aligns with [Microsoft's official GameInput API](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/gameinput_members). Historical changes (e.g. initialization and enumeration fixes) are recorded in **[CHANGELOG.md](CHANGELOG.md)**.

**Reference:** [GameInput API members](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/gameinput_members) | [GameInputCreate](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/functions/gameinputcreate) | [GameInputDeviceInfo](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/structs/gameinputdeviceinfo) | [IGameInput](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/interfaces/igameinput/igameinput) | [IGameInputDevice](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/interfaces/igameinputdevice/igameinputdevice)

---

## Current alignment

### Entry point and initialization

- The wrapper uses **GameInputCreate(IGameInput\*\*)** as the only entry point. The DLL is loaded, GameInputCreate is resolved and called, and the returned pointer is marshalled to IGameInput via GetObjectForIUnknown. GameInputInitialize is not used.

### Device enumeration

- **RegisterDeviceCallback** is called with a device kind filter: **GameInputKindGamepad | GameInputKindController | GameInputKindKeyboard | GameInputKindMouse** so gamepads, keyboards, and mice are enumerated. GameInputKindUnknown is not used for enumeration.

### GameInputDeviceInfo and strings

- [GameInputDeviceInfo](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/structs/gameinputdeviceinfo) documents that all string members use **UTF-8** encoding; `displayName` and `pnpPath` are `const char*`. Offsets in code follow the documented field order (vendorId through containerId, then displayName/pnpPath pointers). Any string read from native uses **UTF-8** via `PtrToUtf8`. On PC, the wrapper does **not** read the displayName or pnpPath pointers (doing so can cause AccessViolation); those fields are returned as empty strings. DeviceId and other fields are read and work as documented.

---

## Verified against documentation

| Area | Our usage | Doc / notes |
|------|-----------|-------------|
| **Entry point** | GameInputCreate(out IntPtr) | GameInputCreate(IGameInput**); we marshal ppv to IGameInput via GetObjectForIUnknown. |
| **IGameInput** | GetCurrentTimestamp, GetCurrentReading, GetNextReading, GetPreviousReading, RegisterDeviceCallback, RegisterReadingCallback, RegisterSystemButtonCallback, RegisterKeyboardLayoutCallback, StopCallback, UnregisterCallback, CreateDispatcher, FindDeviceFromId, FindDeviceFromPlatformString, SetFocusPolicy, CreateAggregateDevice, DisableAggregateDevice | Method names and intent match; vtable order in GameInputComInterfaces.cs must match native (IUnknown + interface methods). |
| **IGameInputDevice** | GetDeviceInfo, GetHapticInfo, GetDeviceStatus, CreateForceFeedbackEffect, IsForceFeedbackMotorPoweredOn, SetForceFeedbackMotorGain, SetRumbleState, DirectInputEscape, CreateInputMapper, GetExtraAxisCount/GetExtraButtonCount/GetExtraAxisIndexes/GetExtraButtonIndexes, CreateRawDeviceReport, SendRawDeviceOutput | Method set matches doc; parameter types and order must match native. |
| **IGameInputDispatcher** | Dispatch(quotaMicroseconds), OpenWaitHandle | Matches doc. |
| **GameInputDeviceInfo** | Offsets for vendorId, productId, usage, versions, deviceId, deviceRootId, deviceFamily, supportedInput, supportedRumbleMotors, supportedSystemButtons, containerId, displayName, pnpPath, then subtype info pointers | Layout comment and constants reference the official struct; displayName/pnpPath are not read on PC to avoid crashes. |
| **Callbacks** | GameInputDeviceCallback, GameInputReadingCallback, GameInputSystemButtonCallback, GameInputKeyboardLayoutCallback | Names and usage match Register* and UnregisterCallback. |
| **Constants** | GameInputKind*, GameInputDeviceConnected, GameInputBlockingEnumeration, etc. | Sourced from GameInputNative.cs / Native/*; values match GameInput.h. |

---

## Gaps and future checks

1. **Display name and pnpPath on PC**  
   Reading `displayName`/`pnpPath` from GameInputDeviceInfo on the PC runtime can cause AccessViolation. The wrapper returns empty strings until a safe read path exists (e.g. confirmed layout for the NuGet runtime or a small native probe). Official doc: UTF-8, layout as in GameInputDeviceInfo.

2. **COM vtable order**  
   IGameInput and IGameInputDevice method order in GameInputComInterfaces.cs must match the native vtable (QueryInterface, AddRef, Release plus interface methods in the same order as the C++ header). When adding or reordering methods, verify against the SDK header or a known-good def.

3. **Struct layouts**  
   GameInputVersion, GameInputUsage, AppLocalDeviceId, GameInputHapticInfo, force feedback params, reading state structs, etc. — layout and size must match the native headers. Any mismatch can cause wrong data or crashes when passing structs to native (e.g. GetHapticInfo, CreateForceFeedbackEffect).

4. **New APIs**  
   When updating to a newer GameInput version, cross-check [GameInput API members](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/gameinput_members) and add/update interfaces, constants, and wrappers accordingly.

---

## Checklist for future changes

- [ ] Any new GameInput entry point or COM method: confirm name and signature against Microsoft docs (learn.microsoft.com/gaming/gdk/docs/reference/input/gameinput/...).
- [ ] Structs passed to or from native: verify layout (field order, sizes, padding) against GameInput.h / docs.
- [ ] String encoding: GameInputDeviceInfo and other doc-specified strings are UTF-8 unless stated otherwise.
- [ ] Enums and constants: use values from official docs or GameInput.h to avoid mismatches with the runtime.
