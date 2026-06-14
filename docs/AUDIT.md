# GameInputSharp.Core — audit summary

This document summarizes whether **GameInputSharp.Core** meets its goal of being a comprehensive C# wrapper for GameInput (game and desktop input) and identifies potential problems or bugs for developers who use it.

**API alignment with Microsoft docs:** Initialization, enumeration, device info layout, and COM method signatures align with the official GameInput API. See **[API_ALIGNMENT.md](API_ALIGNMENT.md)** for current alignment (entry point, device kind filter, display name handling) and a checklist when changing interop or adding APIs. Historical changes are in **[CHANGELOG.md](CHANGELOG.md)**.

**Security and safety:** For risks (DLL loading, buffer handling, callbacks) and a hardening plan, see **[SECURITY.md](SECURITY.md)**.

---

## 1. Goal: comprehensive C\# wrapper for GameInput

### What the wrapper covers

| Area | Coverage | Notes |
|------|----------|--------|
| **Initialization** | ✓ | Dynamic load of GameInput.dll / GameInputRedist.dll; graceful failure (empty devices) when runtime missing. |
| **Device enumeration** | ✓ | Blocking enumeration via RegisterDeviceCallback + dispatcher; gamepad, keyboard, mouse; full device info (GetDeviceInfo), stable ID, and input mapper (axis/button mapping). |
| **Gamepad state** | ✓ | GetCurrentReading + GetGamepadStateFromReading; buttons, triggers, thumbsticks. |
| **Mouse state** | ✓ | GetCurrentReading + GetMouseStateFromReading; buttons, positions, wheel. |
| **Keyboard state** | ✓ | GetKeyCountFromReading + GetKeyStateFromReading; scan code, code point, virtual key, dead key. |
| **Basic haptics** | ✓ | SetRumbleState (left/right rumble). |
| **Advanced haptics** | ✓ | CreateForceFeedbackEffect (constant effect), motor index 0–7, envelope/magnitude. |
| **Device callbacks** | ✓ | Async device connect/disconnect via RegisterDeviceCallback + DispatchCallbacks; event + queue. |
| **Lifecycle** | ✓ | Release of devices, readings, callbacks, GCHandles; ObjectDisposedException on use-after-dispose. |

### Full GameInput API surface (coverage)

| API area | Exposed | Notes |
|----------|---------|--------|
| **IGameInput:** GetCurrentTimestamp | ✓ | `GameInputManager.GetCurrentTimestamp()` |
| FindDeviceFromId / FindDeviceFromPlatformString | ✓ | `FindDeviceFromId(byte[])`, `FindDeviceFromPlatformString(string)` |
| SetFocusPolicy | ✓ | `SetFocusPolicy(uint)` |
| CreateAggregateDevice / DisableAggregateDevice | ✓ | `CreateAggregateDevice(uint, out byte[])`, `DisableAggregateDevice(byte[])` |
| GetNextReading / GetPreviousReading | ✓ | `GameInputInterop` (caller must release reading) |
| **IGameInputDevice:** GetDeviceStatus | ✓ | `GamepadDevice/KeyboardDevice/MouseDevice.GetDeviceStatus()` |
| GetHapticInfo | ✓ | `GamepadDevice.GetHapticInfo()` → `HapticInfo` |
| SetForceFeedbackMotorGain / IsForceFeedbackMotorPoweredOn | ✓ | On `GamepadDevice` |
| GetExtraAxisCount/GetExtraButtonCount, GetExtraAxisIndexes/GetExtraButtonIndexes | ✓ | On all devices (gamepad, keyboard, mouse) |
| DirectInputEscape, CreateInputMapper, CreateRawDeviceReport, SendRawDeviceOutput | ✓ | On all devices |
| RegisterReadingCallback | ✓ | `RegisterReadingCallback(device, inputKind)`, `ReadingCallback` event, pump via `DispatchCallbacks` |
| RegisterSystemButtonCallback, RegisterKeyboardLayoutCallback | ✓ | `RegisterSystemButtonCallback`, `RegisterKeyboardLayoutCallback`, `SystemButtonPressed`, `KeyboardLayoutChanged` events |
| **Readings:** GetSensorsState | ✓ | `GetCurrentSensorsState(GamepadDevice)` |
| GetArcadeStickState, GetFlightStickState, GetRacingWheelState | ✓ | `GetCurrentArcadeStickState`, etc. |
| GetReadingTimestamp / GetReadingInputKind | ✓ | `GameInputManager.GetReadingTimestamp(reading)`, `GameInputInterop` for raw |

**Verdict:** The wrapper exposes the **full v3 GameInput API surface** used by the C# layer for typical scenarios (devices, readings, callbacks, haptics, raw device, focus, aggregation). The small gaps below are optional/low-level and do not block normal use.

### 1.1 Gaps vs. C++ Microsoft.GameInput NuGet (v3 API)

Comparison is against the **current (v3)** GameInput API as documented in the GDK and used by the Microsoft.GameInput NuGet (GameInput::v3). The older **v0** interface (different COM IIDs) exposes additional device methods (e.g. GetBatteryState, AcquireExclusiveRawDeviceAccess, PowerOff); this wrapper does not implement the v0 interface.

| C++ API | Exposed in C#? | Notes |
|--------|----------------|--------|
| **IGameInputDispatcher::OpenWaitHandle** | ✓ | `CreateDispatcherWaitHandle()` returns `DispatcherWaitHandle` with `SafeWaitHandle`; wait on it then call `DispatchCallbacks`. |
| **IGameInputReading::GetDevice** | ✓ | `GetDeviceFromReading(reading)`; caller must release the returned device pointer. |
| **IGameInputReading::GetControllerAxisCount / GetControllerAxisState** | ✓ | `GetControllerAxisCount(reading)`, `GetControllerAxisState(reading, float[])`. |
| **IGameInputReading::GetControllerButtonCount / GetControllerButtonState** | ✓ | `GetControllerButtonCount(reading)`, `GetControllerButtonState(reading, uint[])`. |
| **IGameInputReading::GetControllerSwitchCount / GetControllerSwitchState** | ✓ | `GetControllerSwitchCount(reading)`, `GetControllerSwitchState(reading, int[])`. |
| **IGameInputReading::GetRawReport** | ✓ | `GetRawReportFromReading(reading, out IntPtr)`; do not release, valid only for reading lifetime. |
| **IGameInput::StopCallback** | ✓ | `StopCallback(callbackToken)`; tokens from `RegisterReadingCallback(..., out token)` etc. |

All v3 reading and dispatcher APIs listed above are exposed. Callback registration overloads return a token for use with `StopCallback(token)` (fire-and-forget stop) or `UnregisterCallback(token)` (wait for completion).

### 1.2 Remaining gaps (optional / advanced)

These v3 APIs are either not exposed or only partially exposed. They are optional for typical game/desktop input; add if you need full parity for advanced scenarios.

| C++ API / area | Exposed in C#? | Notes |
|----------------|----------------|--------|
| **IGameInputMapper** (methods) | ✓ | `CreateInputMapper()` returns an `InputMapper` wrapper (dispose when done). All mapper methods are exposed: `GetGamepadAxisMappingInfo`, `GetGamepadButtonMappingInfo`, `GetFlightStickAxisMappingInfo`, `GetFlightStickButtonMappingInfo`, `GetRacingWheelAxisMappingInfo`, `GetRacingWheelButtonMappingInfo`, `GetArcadeStickButtonMappingInfo`. Use `AxisMappingInfo` and `ButtonMappingInfo` for results. |
| **IGameInputForceFeedbackEffect** (as object) | ✓ | `GamepadDevice.CreateForceFeedbackEffect` returns a `ForceFeedbackEffect` wrapper (Start/Pause/Stop, SetGain, State, MotorIndex). **All 11 effect kinds** are exposed: Constant, Ramp, SineWave, SquareWave, TriangleWave, SawtoothUpWave, SawtoothDownWave, Spring, Friction, Damper, Inertia. Use `ForceFeedbackConstantParams`, `ForceFeedbackRampParams`, `ForceFeedbackPeriodicParams`, or `ForceFeedbackConditionParams` with the appropriate overload. Dispose the effect when done. |
| **GameInputDeviceInfo** (full struct) | ✓ | `IInputDevice.GetDeviceInfo()` returns a `DeviceInfo` instance with vendorId, productId, revisionNumber, usage, hardwareVersion, firmwareVersion, deviceId/deviceRootId bytes, deviceFamily, supportedInput, supportedRumbleMotors, supportedSystemButtons, containerId, displayName, pnpPath, Has* flags for nested info (keyboard/mouse/gamepad/forceFeedbackMotorInfo/inputReportInfo/outputReportInfo), and ForceFeedbackMotorCount/InputReportCount/OutputReportCount. |
| **IGameInputRawDeviceReport** (interface) | ✓ | `CreateRawDeviceReport()` returns a `RawDeviceReport` wrapper (dispose when done). The wrapper exposes `GetReportInfo()`, `GetRawDataSize()`, `GetRawData(byte[])`, `SetRawData(byte[])`, and `UnsafePointer`. Use `device.SendRawDeviceOutput(report)` to send; the report is not disposed by that call. |
| **Typed state from a reading handle** | ✓ | `GameInputManager` exposes `GetGamepadStateFromReading(reading)`, `GetMouseStateFromReading(reading)`, `GetKeyboardStateFromReading(reading, maxKeys)`, `GetSensorsStateFromReading(reading)`, `GetArcadeStickStateFromReading(reading)`, `GetFlightStickStateFromReading(reading)`, and `GetRacingWheelStateFromReading(reading)`. Use these in a reading callback to obtain typed state from `ReadingCallbackEventArgs.Reading` without polling. |

---

## 2. Potential problems and bugs for developers

### High impact

1. **Device callback and device identity**
   `DeviceCallbackEventArgs` exposes `DeviceId` (same value as `IInputDevice.DeviceId`) so callers can correlate without re-enumerating. Re-enumeration remains an option if you need the full device list after a change.

2. **GetDevices() returns new wrappers every time**
   Each call to `GetDevices()` runs a new enumeration and returns **new** wrapper instances (new refs to the same physical devices). If a developer caches “the gamepad” from an earlier call and later calls `GetDevices()` again and disposes only the new list, the old cached wrapper may still hold a ref; if they then dispose that cached wrapper, they release one ref. There is no double-release of the same pointer, but **identity** is not stable across calls (two different `GamepadDevice` instances can represent the same physical device). Use `DeviceId` to correlate, or **TryGetDeviceByDeviceId(deviceId)** to obtain a wrapper for a known ID (e.g. from `DeviceCallbackEventArgs.DeviceId`) without holding a long-lived reference.

### Medium impact

1. **Thread safety**
   -  `GameInputManager` class remarks and [USAGE.md](USAGE.md) state that the manager is not thread-safe; use from a single thread (e.g. game loop) or synchronize externally. Do not call `Dispose` from inside a `DeviceCallback` or `ReadingCallback`.
   - Device callbacks are invoked from the thread that calls `DispatchCallbacks` (dispatcher thread). If handlers do heavy work or touch UI, they may need to marshal to the correct thread.

2. **Dispose race with device callbacks**
   On `GameInputManager.Dispose()`, we unregister the callback then free the `GCHandle`. A callback could theoretically still run (e.g. one already in the dispatcher). The static callback uses `try/catch` and checks `handle.Target`; if the handle is freed, behavior depends on `GCHandle.FromIntPtr` and `Target`. Unregistering first minimizes the window; documenting “do not call Dispose from inside a callback” and “call DispatchCallbacks only from one thread” reduces risk.

3. **Silent failures in interop**
   Several interop helpers swallow exceptions (`catch { }` or `catch { return false; }`), e.g. `SetRumble`, `PlayForceFeedbackConstant`, `GetDeviceInfoFromPtr`. Failures (e.g. device disconnected) can appear as no-ops. **Mitigated:** when an `ILogger` is passed to `GameInputManager`, the wrapper logs key failure paths (e.g. `RegisterReadingCallback` / `RegisterSystemButtonCallback` / `RegisterKeyboardLayoutCallback` returning false, `GetGamepadStateFromReading` failing despite a valid reading). Check return values where provided and use `GameInputManager(ILogger)` for diagnostics.

### Lower impact

1. **Gamepad button bitmask**
   `GamepadState.Buttons` is a raw `uint` bitmask. The library provides **GameInputGamepadButtons** (e.g. `A`, `B`, `DPadUp`) for testing bits. See [MAPPING_REFERENCE.md](MAPPING_REFERENCE.md) and `GamepadState` / `InputState` XML for usage.

2. **Reading timestamp**
   `GameInputManager.GetReadingTimestamp(reading)` returns the reading’s timestamp in microseconds (for latency or replay). Use with `GetGamepadStateFromReading` / `GetMouseStateFromReading` / etc. when processing a reading from a callback.

---

## 3. Recommendations (current state)

- **Ownership and disposal:** [USAGE.md](USAGE.md) states that the caller owns device wrappers from `GetDevices()` and must dispose them; it also covers thread safety and callback rules.
- **DeviceCallbackEventArgs:** Event args include `DeviceId` (same as `IInputDevice.DeviceId`); use **TryGetDeviceByDeviceId(deviceId)** to get a wrapper for a known ID.
- **Device wrappers:** `GamepadDevice`, `KeyboardDevice`, and `MouseDevice` have finalizers that release the native pointer if `Dispose()` is not called.
- **Thread safety and disposal:** Manager class remarks and USAGE.md state that the manager is not thread-safe, to use a single thread or external sync, and not to call `Dispose` from inside a callback.
- **Reading timestamp:** `GameInputManager.GetReadingTimestamp(reading)` returns the reading’s timestamp in microseconds for use with state-from-reading APIs.
- **Logging:** `GameInputManager(ILogger)` is supported; the wrapper logs init/device-wrap failures and, when a logger is set, key registration and state-extraction failures (Debug level).

---

## 4. Conclusion

- **Goal:** **GameInputSharp.Core** is **comprehensive for its scope**: game/desktop input, haptics (basic and advanced, up to 8 locations per device), reading and device callbacks, sensors/motion, raw device reports, and native aggregate devices. Remaining Core gaps are optional v3 APIs (see §1.1) and the separate v0 device interface (e.g. GetBatteryState, exclusive raw access).
- **Risks:** The main developer-facing risks are mitigated: (1) device identity via `DeviceCallbackEventArgs.DeviceId` and **TryGetDeviceByDeviceId**; (2) thread safety and callback rules in class remarks and USAGE.md; (3) disposal ownership documented; (4) reading timestamp via **GetReadingTimestamp(reading)**; (5) optional logging for key failure paths when `GameInputManager(ILogger)` is used. Device wrappers have finalizers to avoid ref leaks.
