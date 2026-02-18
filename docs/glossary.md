# Glossary

Short definitions of terms used in the GameInputSharp.Core documentation and API.

---

## A–C

**Aggregate device** — A virtual device created by the GameInput API that combines input from multiple physical devices (e.g. multiple gamepads). Created with `CreateAggregateDevice`; use `FindDeviceFromId` with the returned ID and `DisableAggregateDevice` when done.

**App-local device ID** — A 32-byte identifier for a device in the current process. Exposed as `IInputDevice.DeviceId` (hex string) or as `byte[]` for `FindDeviceFromId` / `DisableAggregateDevice`. Not guaranteed to be stable across app restarts.

**Callback token** — An opaque value returned by `RegisterReadingCallback`, `RegisterSystemButtonCallback`, or `RegisterKeyboardLayoutCallback`. Pass to `StopCallback` (fire-and-forget stop) or `UnregisterCallback` (wait for in-flight callbacks to complete).

**DispatchCallbacks** — Method on `GameInputManager` that processes pending device and reading callbacks. Call it from your game loop (e.g. every frame) so `DeviceCallback` and `ReadingCallback` events are raised.

**GameInput** — Microsoft’s cross-platform input API (C++/COM). GameInputSharp.Core is a C# wrapper around it. The runtime is implemented by GameInput.dll / GameInputRedist.dll on Windows.

**GameInputManager** — The main entry point in this wrapper. Creates and owns the native GameInput instance; provides `GetDevices()`, polling APIs, callbacks, and haptics. Implements `IDisposable`; dispose when done.

**GCHandle** — .NET handle used to pass managed state to native callbacks. The wrapper uses it so native code can invoke managed callback queues. Callbacks are unregistered before the handle is freed on `Dispose`.

---

## D–H

**DeviceCallback** — Event on `GameInputManager` raised when a device connects or disconnects. Subscribe and call `DispatchCallbacks` each frame to receive events without re-enumerating.

**DirectInput escape** — A way to send custom commands to a device (e.g. vendor-specific). Wrapper exposes `DirectInputEscape(bufferIn, bufferOut)` with size limits (see [Security](SECURITY.md)).

**Force feedback** — Effects that drive motors on the device (rumble, constant force, periodic waves, spring/friction/damper). Exposed via `GameInputDevice.Haptics` (simple rumble) and `AdvancedHaptics` / `CreateForceFeedbackEffect` (full effect kinds).

**Focus policy** — Controls whether the app receives input when in background. Set with `SetFocusPolicy(GameInputFocusPolicy.Default | Background | Exclusive)`.

**Haptic location** — A motor or actuator on the device. Up to 8 per device (`HapticLocation.LeftLowFrequency`, `RightHighFrequency`, `Location2`–`Location7`). Used with `PlayHapticWaveform` and advanced effects.

---

## I–R

**Input kind** — A flag indicating the type of input (gamepad, keyboard, mouse, flight stick, racing wheel, etc.). `GameInputKinds` constants; used for polling, callbacks, and aggregate device creation.

**Reading** — A single snapshot of input at a point in time. Obtained by polling (`GetCurrentGamepadState`, etc.) or in a `ReadingCallback`. Use `GetGamepadStateFromReading(args.Reading)` and similar; dispose the reading handle when done.

**ReadingCallback** — Event raised when new input readings are available for a device/input kind. Register with `RegisterReadingCallback(device, inputKind)` and call `DispatchCallbacks` each frame.

**Re-entrancy** — Calling back into the wrapper (e.g. `UnregisterCallback` or `Dispose`) from inside a callback handler. Not allowed; the wrapper throws if detected. Unregister or dispose from the main loop instead.

**Rumble** — Simple left/right motor vibration. `SetVibration(left, right)` with values 0–1. For more control, use advanced haptics or force-feedback effects.

---

## S–Z

**System32-only load** — Option to load GameInput.dll only from `Environment.SystemDirectory`, not from the app directory or search path. Use `new GameInputManager(logger, loadOnlyFromSystem32: true)` for stricter security.

**Thread safety** — The manager is **not** thread-safe. Use from a single thread (e.g. game loop) or synchronize externally. Call `DispatchCallbacks` only from one thread.

**Wrapper** — GameInputSharp.Core: the C# library that wraps the native GameInput API so you can call it from .NET without writing COM/P-Invoke yourself.

**XInput** — Microsoft’s older gamepad API. Some controllers are exposed as XInput or Windows.Gaming.Input but not (or not always) via GameInput. If you see 0 devices with init OK, try USB or see [Compatibility](COMPATIBILITY.md).
