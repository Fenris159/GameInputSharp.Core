# Security and safety — GameInputSharp.Core

This document describes **what is implemented** for security and safety in the GameInputSharp wrapper. It does **not** cover the Microsoft GameInput runtime itself; only how the wrapper uses it and how developer- or attacker-controlled input can affect the process.

**Scope:** The wrapper performs no network I/O, no script/eval, and no execution of user-supplied code. Implemented mitigations address: (1) native DLL loading, (2) marshalling and buffer handling with native code, (3) callback and `GCHandle` lifetime, and (4) APIs that accept developer- or device-derived data (strings, byte arrays, counts). For a history of security-related changes, see [CHANGELOG.md](CHANGELOG.md).

---

## 1. Risk areas and implemented mitigations

### 1.1 DLL loading (code injection / hijacking)

| Risk | Location | Mitigation implemented |
|------|----------|-------------------------|
| **DLL search order** | `GameInputInterop.TryLoadGameInputDll` | Load order: (1) `Environment.SystemDirectory` (GameInput.dll, GameInputRedist.dll), (2) `AppContext.BaseDirectory`, (3) default search path. For strictest control, **load-only-from-System32** is available: `GameInputManager(logger, loadOnlyFromSystem32: true)` loads only from System32 and fails if the DLL is not there (`TryLoadGameInputDllFromSystem32Only`, `TryCreateGameInput(loadOnlyFromSystem32)`). |
| **Path disclosure** | `GetLoadPaths`, `GetLastLoadError`, `GetMainPathLoadFailure` | These diagnostic APIs intentionally expose paths and error messages for support. In high-security or sandboxed environments, avoid exposing them to untrusted callers. |

---

### 1.2 Native interop and pointer handling

| Risk | Location | Mitigation implemented |
|------|----------|-------------------------|
| **Invalid pointers** | `PtrToUtf8`, `Marshal.Read*`/`Marshal.Copy` over `IntPtr` | The wrapper **does not** read `displayName`/`pnpPath` from `GameInputDeviceInfo` on PC to avoid reading through invalid pointers. |
| **COM vtable / method order** | `GameInputComInterfaces.cs` | Documented and verified; see [API_ALIGNMENT.md](API_ALIGNMENT.md). |
| **Struct layout mismatch** | Structs passed to/from native | Layout documented and verified where possible. |

---

### 1.3 Buffer and size handling (DoS / memory exhaustion)

| Risk | Location | Mitigation implemented |
|------|----------|-------------------------|
| **Unbounded allocation from native count** | `GetExtraAxisIndexes`, `GetExtraButtonIndexes` | Native-derived `count` is **capped at 1024** before allocation. |
| **Large caller-supplied buffers** | `DirectInputEscape(bufferIn, bufferOut)` | **64 KB maximum** per buffer; over limit returns failure. |
| **Large caller-supplied buffers** | `RawDeviceReport.SetRawData(buffer)`, `GetRawData(buffer)` | **8192 bytes** maximum; over limit returns `false` or `(false, 0)`. |
| **Keyboard key count** | `GetKeyboardStateFromReading(reading, maxKeys)`, `GetCurrentKeyboardState` | **maxKeys capped at 1024**; default remains 256. |

---

### 1.4 String and platform input (native API abuse)

| Risk | Location | Mitigation implemented |
|------|----------|-------------------------|
| **Platform string passed to native** | `FindDeviceFromPlatformString(platformString)` | **2048 character** maximum; longer strings cause the API to return `IntPtr.Zero`. |

---

### 1.5 Callbacks and GCHandle lifetime

| Risk | Location | Mitigation implemented |
|------|----------|-------------------------|
| **Use-after-free of GCHandle** | Callbacks using `GCHandle.FromIntPtr(context)` | Callbacks are **unregistered before** the manager disposes and frees context handles. |
| **Re-entrancy** | Callback handlers calling back into the wrapper | **Re-entrancy guard:** `UnregisterCallback` and `Dispose` throw `InvalidOperationException` if invoked from inside a `DeviceCallback` or `ReadingCallback` handler. USAGE.md and XML docs state that callbacks must not call `Dispose` or `UnregisterCallback` from within a callback. |

---

## 2. What the wrapper does not do (no finding)

- **No code injection from data:** The wrapper does not interpret user or device data as code (no eval, no dynamic compilation, no loading of paths from device strings).
- **No network:** No sockets or HTTP; no data sent off-host by the library.
- **No file I/O of device data:** The wrapper does not write device identifiers or input state to disk.
- **Display name / pnp path:** Not read from native on PC, so no risk of reading through invalid pointers for those fields at runtime.

---

## 3. Device ID validation

- **FindDeviceFromId** and **DisableAggregateDevice** require `deviceIdBytes` length ≥ 32 (`AppLocalDeviceId.Size`). **All-zero** device IDs are **rejected** (no native call); see XML and USAGE.md.

---

## 4. Summary

- **No classic code injection** (no eval, no execution of user data). Addressed risks: **DLL hijacking** (load order and optional System32-only mode), **memory exhaustion / DoS** (caps on buffers and native-derived counts), and **callback/lifetime** (unregister-before-dispose, re-entrancy guard, all-zero device ID rejection).
- **Ongoing:** When adding new interop APIs (especially byte[], string, or counts from native), reassess buffer limits and document any new risks or caps in this document. Security relies on documented caps and behavior, not obscurity.
