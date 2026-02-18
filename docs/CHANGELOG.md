# Changelog — GameInputSharp.Core

All notable changes to the **GameInputSharp.Core** package are documented here. This changelog is specific to the Core package only; for **GameInputSharp.Enterprise** see that package's repository and its own `docs/CHANGELOG.md`.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Security and safety hardening

The following improvements were implemented so that the wrapper’s security posture matches [docs/SECURITY.md](docs/SECURITY.md). That document is reframed as **what is implemented** (current mitigations); this changelog records the changes that made it possible.

#### Buffer and size caps (DoS / memory exhaustion)

- **Native-derived counts:** In `GetExtraAxisIndexes` and `GetExtraButtonIndexes`, the `count` from native is **capped at 1024** before allocating `uint[count]` or `AllocHGlobal`. Prevents memory exhaustion from buggy or malicious drivers.
- **DirectInputEscape:** `bufferIn` and `bufferOut` are each limited to **64 KB**. Requests over the limit return failure (e.g. `(false, 0)`). Reduces DoS from large caller-supplied buffers.
- **RawDeviceReport:** `SetRawData(buffer)` and `GetRawData(buffer)` are limited to **8192 bytes**. Over limit returns `false` or `(false, 0)`.
- **Platform string:** `FindDeviceFromPlatformString(platformString)` rejects strings longer than **2048 characters** (returns `IntPtr.Zero`). Reduces stress or undefined behavior in the native API.
- **Keyboard maxKeys:** In `GetKeyboardStateFromReading(reading, maxKeys)` and `GetCurrentKeyboardState(device, maxKeys)`, `maxKeys` is **capped at 1024**. Default remains 256.

#### DLL loading

- **System32-only option:** Added `TryLoadGameInputDllFromSystem32Only(out IntPtr handle)` and `TryCreateGameInput(bool loadOnlyFromSystem32)` in `GameInputInterop`. When `loadOnlyFromSystem32` is true, the DLL is loaded only from `Environment.SystemDirectory`; if not found, init fails.
- **GameInputManager:** New constructor overload `GameInputManager(ILogger? logger, bool loadOnlyFromSystem32)`. Use `new GameInputManager(logger, loadOnlyFromSystem32: true)` for maximum protection against DLL hijacking. Documented in USAGE.md “Security and safety” and SECURITY.md.

#### Callbacks and lifetime

- **Documentation:** USAGE.md now includes a “Security and safety” subsection: callback lifetime (wrapper unregisters callbacks then frees context), rule not to call `Dispose` or `UnregisterCallback` from inside a callback, and link to SECURITY.md. XML on `GameInputManager` and `UnregisterCallback`/`Dispose` updated with the same rules and reference to docs/SECURITY.md.
- **Re-entrancy guard:** In `GameInputManager`, a flag is set while `DeviceCallback`, `ReadingCallback`, `SystemButtonPressed`, or `KeyboardLayoutChanged` is being invoked. If `UnregisterCallback(ulong)` or `Dispose()` is called while that flag is set, the wrapper throws `InvalidOperationException` with a message pointing to SECURITY.md. Prevents undefined behavior from the Microsoft API when callers re-enter from a callback.

#### Device ID validation

- **All-zero rejection:** `FindDeviceFromId` and `DisableAggregateDevice` (in both `GameInputInterop` and `GameInputManager`) now **reject all-zero** device IDs: no native call is made and the API returns `IntPtr.Zero` or `false`. Length requirement (≥ 32 bytes) was already enforced; all-zero check reduces accidental or nonsensical lookups. XML and SECURITY.md updated.

#### Documentation

- **SECURITY.md** reframed as “what is” implemented: risk areas with a single “Mitigation implemented” column, no pending task list. Removed the former “Hardening plan” tables; all completed items are recorded in this changelog.
- **USAGE.md:** “Security and safety” subsection and expanded “Thread safety and callback rules” (including “do not call UnregisterCallback from inside that same callback”).

---

## 2026-02 — API alignment and initialization fixes

### Changed

- **Initialization:** The wrapper uses the official **GameInputCreate(IGameInput\*\*)** API only. The undocumented GameInputInitialize path is not used; the DLL is loaded and GameInputCreate is resolved and called so init matches the Microsoft.GameInput PC runtime and other apps (e.g. EDForceFeedbackXinput).
- **Device enumeration:** Enumeration uses a proper device kind filter (**GameInputKindGamepad | GameInputKindController | GameInputKindKeyboard | GameInputKindMouse**) so connected gamepads, keyboards, and mice are reported. Previously using GameInputKindUnknown (0) resulted in no devices.
- **GameInputDeviceInfo:** Layout and string encoding follow the official GameInputDeviceInfo documentation (UTF-8 for string members). On PC, the wrapper does not read the `displayName` or `pnpPath` pointers from the struct to avoid AccessViolation; those fields are returned as empty strings. DeviceId and other fields are unchanged and work as documented.

### Added

- **docs/API_ALIGNMENT.md** — Documents how the wrapper aligns with Microsoft's official GameInput API (entry point, COM methods, struct layout, checklist for future changes). Historical details of the above corrections are recorded in this changelog.
