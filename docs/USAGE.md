# GameInputSharp.Core — game and desktop usage

This guide is for **game developers and PC application developers** using GameInputSharp.Core for input devices (gamepads, keyboards, mice) and haptics on Windows. It covers device enumeration, polling, rumble, advanced haptics, and device connect/disconnect callbacks.

---

## Installation and requirements

1. **Add the NuGet packages** to your project:

   ```xml
   <ItemGroup>
     <PackageReference Include="GameInputSharp.Core" Version="1.0.0" />
     <PackageReference Include="Microsoft.GameInput" Version="3.4.218" />
   </ItemGroup>
   ```

2. **Target .NET 8+** and **Windows** for full support (e.g. `net8.0-windows`). The library builds on other targets but returns no devices when not running on Windows.

3. **GameInput runtime:** [GameInput.dll](https://learn.microsoft.com/en-us/gaming/gdk/) (or GameInputRedist.dll) must be available on the machine. If it is missing, `GetDevices()` returns an empty list and does not throw.

---

## Manager and device enumeration

Create a `GameInputManager` and enumerate devices. Prefer `using` so the manager is disposed and native resources are released.

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

using var manager = new GameInputManager();
IReadOnlyList<IInputDevice> devices = manager.GetDevices();

foreach (var device in devices)
{
    Console.WriteLine($"{device.DisplayName} — {device.DeviceId}");
    Console.WriteLine($"  Connected: {device.IsConnected}");

    if (device is GamepadDevice gamepad)
        Console.WriteLine("  Type: Gamepad");
    else if (device is KeyboardDevice keyboard)
        Console.WriteLine("  Type: Keyboard");
    else if (device is MouseDevice mouse)
        Console.WriteLine("  Type: Mouse");
}
```

Device references hold native handles. **You own them:** dispose each device when done, or scope use to the manager’s lifetime. Each call to `GetDevices()` returns new wrapper instances; if you call it multiple times, prefer one list and reuse it, or dispose previous devices before enumerating again to avoid mixing identities (use `DeviceId` to correlate if needed).

---

## Polling gamepad state

Poll the current gamepad state each frame. Use a `GamepadDevice` from `GetDevices()`.

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

using var manager = new GameInputManager();
var gamepad = manager.GetDevices().OfType<GamepadDevice>().FirstOrDefault();
if (gamepad == null)
    return;

// In your game loop:
GamepadState? state = manager.GetCurrentGamepadState(gamepad);
if (state == null)
    return; // no reading for this frame

// Triggers [0,1] and thumbsticks [-1,1]
float leftTrigger  = state.Value.LeftTrigger;
float rightTrigger = state.Value.RightTrigger;
float leftX        = state.Value.LeftThumbstickX;
float leftY        = state.Value.LeftThumbstickY;
float rightX       = state.Value.RightThumbstickX;
float rightY       = state.Value.RightThumbstickY;

// Buttons: bitmask — use GameInputGamepadButtons (see MAPPING_REFERENCE.md)
bool aPressed = (state.Value.Buttons & (uint)GameInputGamepadButtons.A) != 0;
bool bPressed = (state.Value.Buttons & (uint)GameInputGamepadButtons.B) != 0;
```

Use the `GameInputGamepadButtons` enum (e.g. `GameInputGamepadButtons.A`, `GameInputGamepadButtons.DPadUp`) for the button bitmask; see [MAPPING_REFERENCE.md](MAPPING_REFERENCE.md) for the full list of axes and buttons for all device types.

---

## Polling mouse state

Use a `MouseDevice` from `GetDevices()` and poll each frame.

```csharp
var mouse = manager.GetDevices().OfType<MouseDevice>().FirstOrDefault();
if (mouse == null)
    return;

MouseState? state = manager.GetCurrentMouseState(mouse);
if (state == null)
    return;

uint buttons = state.Value.Buttons;
long relX = state.Value.PositionX;
long relY = state.Value.PositionY;
long absX = state.Value.AbsolutePositionX;
long absY = state.Value.AbsolutePositionY;
long wheelX = state.Value.WheelX;
long wheelY = state.Value.WheelY;
```

---

## Polling keyboard state

Use a `KeyboardDevice` and request a maximum number of keys per reading.

```csharp
var keyboard = manager.GetDevices().OfType<KeyboardDevice>().FirstOrDefault();
if (keyboard == null)
    return;

const int maxKeys = 32;
KeyState[] keys = manager.GetCurrentKeyboardState(keyboard, maxKeys);
if (keys.Length == 0)
    return;

foreach (KeyState key in keys)
{
    uint scanCode = key.ScanCode;
    uint codePoint = key.CodePoint;
    byte virtualKey = key.VirtualKey;
    bool isDeadKey = key.IsDeadKey;
    // Map VirtualKey to your key bindings
}
```

---

## Basic haptics (rumble)

Simple left/right rumble on a gamepad. Values are 0–1; 0 stops the motor.

```csharp
var gamepad = manager.GetDevices().OfType<GamepadDevice>().FirstOrDefault();
if (gamepad == null)
    return;

gamepad.Haptics.SetVibration(0.5f, 0.5f);   // left (low-freq), right (high-freq)
gamepad.Haptics.SetVibration(0f, 0f);       // stop
```

---

## Advanced haptics (waveforms, multiple motors)

Use `AdvancedHaptics` for per-motor effects and optional waveform data (duration + intensity). Each device supports up to 8 haptic locations (`HapticLocation`).

```csharp
using GameInputSharp.Haptics;

var gamepad = manager.GetDevices().OfType<GamepadDevice>().FirstOrDefault();
if (gamepad == null)
    return;

var advanced = gamepad.AdvancedHaptics ?? new AdvancedHaptics(gamepad);

// Optional 8-byte header: 4 bytes duration (ms, little-endian), 4 bytes intensity (float)
byte[]? waveformData = new byte[8];
uint durationMs = 150;
float intensity = 0.7f;
BitConverter.TryWriteBytes(waveformData.AsSpan(0, 4), durationMs);
BitConverter.TryWriteBytes(waveformData.AsSpan(4, 4), intensity);

HapticLocation[] locations = { HapticLocation.LeftLowFrequency, HapticLocation.RightHighFrequency };
advanced.PlayHapticWaveform(waveformData, locations);

// Defaults (100 ms, 0.5 intensity) when waveformData is null
advanced.PlayHapticWaveform(null, new[] { HapticLocation.LeftLowFrequency });
```

`HapticLocation` values 0–7 map to device motors; see the enum for `LeftLowFrequency`, `RightHighFrequency`, and `Location2`–`Location7`.

**Granular control (same as C++ IGameInputForceFeedbackEffect)** — To pause, resume, change gain, or query state of an effect, create a **controllable** effect and keep a reference:

```csharp
using GameInputSharp.Haptics;

// Create effect (not started yet)
using var effect = gamepad.CreateForceFeedbackEffect(
    motorIndex: 0,
    durationMicroseconds: 500_000,  // 0.5 s
    intensity: 0.8f);
if (effect == null)
    return;

effect.Start();           // run
effect.Pause();           // pause (can resume)
effect.Start();           // resume
effect.SetGain(0.5f);     // reduce intensity
if (effect.State == ForceFeedbackEffectState.Running)
    ; // still playing
effect.Stop();            // stop
// Dispose when done (releases native effect)
```

Use this when you need the same control as the C++ API (pause/resume, per-effect gain, state query). For one-shot “fire and forget” use `AdvancedHaptics.PlayHapticWaveform`.

### All force feedback effect kinds (full complexity)

The wrapper exposes **all 11 effect kinds** from the GameInput API: **Constant**, **Ramp**, **SineWave**, **SquareWave**, **TriangleWave**, **SawtoothUpWave**, **SawtoothDownWave**, **Spring**, **Friction**, **Damper**, **Inertia**. Create with the right params, then `Start()` / `Pause()` / `Stop()` / `SetGain()`; **dispose** when done.

- **Constant:** `CreateForceFeedbackEffect(motorIndex, durationMicroseconds, intensity)` or `(motorIndex, in ForceFeedbackConstantParams)`.
- **Ramp:** `(motorIndex, in ForceFeedbackRampParams)` — start/end magnitude.
- **Periodic (sine, square, triangle, sawtooth):** `(motorIndex, ForceFeedbackEffectKind.SineWave|...|SawtoothDownWave, in ForceFeedbackPeriodicParams)` — set `Frequency`, `Phase`, `Bias`.
- **Condition (spring, friction, damper, inertia):** `(motorIndex, ForceFeedbackEffectKind.Spring|Friction|Damper|Inertia, in ForceFeedbackConditionParams)` — set `PositiveCoefficient`, `NegativeCoefficient`, `DeadZone`, `Bias`. Common for racing wheels and flight sticks.

Envelope durations are in **100-nanosecond units** (1 µs = 10 units). See `ForceFeedbackParams.cs` and `ForceFeedbackEffectKind` for all struct fields.

---

## Device connect/disconnect callbacks

Subscribe to `DeviceCallback` and call `DispatchCallbacks` from your game loop so connect/disconnect events are processed without re-enumerating.

```csharp
using var manager = new GameInputManager();

manager.DeviceCallback += (sender, args) =>
{
    // args.DeviceId matches IInputDevice.DeviceId — correlate with your wrappers without re-enumerating
    // args.Timestamp, args.CurrentStatus, args.PreviousStatus (e.g. GameInputDeviceConnected 0x1)
    bool connected = (args.CurrentStatus & 0x1) != 0;
    Console.WriteLine($"Device {args.DeviceId}: Connected={connected}");
};

// In your game loop (e.g. every frame):
manager.DispatchCallbacks(quotaMicroseconds: 1000);
```

`DeviceCallbackEventArgs` exposes `DeviceId` (matches `IInputDevice.DeviceId` for correlation), `Timestamp` (microseconds), and `CurrentStatus` / `PreviousStatus` (native status flags).

---

## Additional input kinds and APIs (full surface)

The wrapper exposes the rest of the GameInput API so C# developers can use all supported input kinds and device features.

**Timestamp and device lookup**

```csharp
ulong timestampUs = manager.GetCurrentTimestamp();

// Find a device by 32-byte app-local ID (e.g. from DeviceId hex-decoded, or from CreateAggregateDevice)
byte[] idBytes = new byte[32]; // fill from your source
IInputDevice? device = manager.FindDeviceFromId(idBytes);
if (device != null) { /* use; dispose when done */ }

// Find by platform string
IInputDevice? dev2 = manager.FindDeviceFromPlatformString(@"\\?\USB#VID_045E&PID_0B13#...");
```

**Focus policy**

```csharp
using GameInputSharp.Abstractions;
manager.SetFocusPolicy(GameInputFocusPolicy.Exclusive);  // or Default, Background
```

**Sensors (motion), arcade stick, flight stick, racing wheel**

Use a device that supports the input kind (often a gamepad or dedicated controller):

```csharp
SensorsState? sensors = manager.GetCurrentSensorsState(gamepad);
ArcadeStickState? arcade = manager.GetCurrentArcadeStickState(gamepad);
FlightStickState? flight = manager.GetCurrentFlightStickState(gamepad);
RacingWheelState? wheel = manager.GetCurrentRacingWheelState(gamepad);
```

**Device status and haptic info (gamepad)**

```csharp
uint status = gamepad.GetDeviceStatus();
bool connected = (status & GameInputDeviceStatus.Connected) != 0;  // see API_REFERENCE.md
HapticInfo haptic = gamepad.GetHapticInfo();  // LocationCount, LocationIds, AudioEndpointId
gamepad.SetForceFeedbackMotorGain(motorIndex: 0, gain: 0.8f);
bool on = gamepad.IsForceFeedbackMotorPoweredOn(0);
```

**Aggregate devices (native)**

```csharp
if (manager.CreateAggregateDevice(GameInputKinds.Gamepad, out byte[] aggregateId))
{
    IInputDevice? agg = manager.FindDeviceFromId(aggregateId);
    // ... use aggregate device ...
    manager.DisableAggregateDevice(aggregateId);
}
```

**GetNextReading / GetPreviousReading** are available on `GameInputInterop` for history/replay; you obtain a reading pointer, call `GetReadingTimestamp` / `GetReadingInputKind`, get state via `GetGamepadStateFromReading` etc., and must call `ReleaseReading` when done.

---

## Reading, system button, and keyboard layout callbacks

**Reading callback** — get notified when new input readings arrive (per device, per input kind):

```csharp
manager.ReadingCallback += (sender, args) =>
{
    using (args.Reading)
    {
        // Typed state from the reading (no polling):
        GamepadState? state = manager.GetGamepadStateFromReading(args.Reading);
        if (state.HasValue)
            ; // use state.Value.Buttons, .LeftTrigger, etc.
        // Or: GetMouseStateFromReading, GetKeyboardStateFromReading, GetSensorsStateFromReading, etc.
        // args.HasOverrunOccurred indicates if some readings were skipped.
    }
};
manager.RegisterReadingCallback(gamepad, GameInputKinds.Gamepad);
// Call DispatchCallbacks() each frame to pump
```

**System button (e.g. Xbox guide)** and **keyboard layout change**:

```csharp
manager.SystemButtonPressed += (s, e) => { /* guide button pressed */ };
manager.RegisterSystemButtonCallback(gamepad, buttonFilter: 0); // 0 = all

manager.KeyboardLayoutChanged += (s, e) => { /* layout changed */ };
manager.RegisterKeyboardLayoutCallback(keyboard);
```

**Callback tokens and StopCallback / UnregisterCallback**

Register overloads can return a callback token so you can stop or unregister later:

```csharp
if (manager.RegisterReadingCallback(gamepad, GameInputKinds.Gamepad, out ulong readingToken))
{
    // Later: stop without waiting for in-flight callbacks (e.g. on shutdown)
    manager.StopCallback(readingToken);
    // Or wait for completion: manager.UnregisterCallback(readingToken);
}
manager.RegisterSystemButtonCallback(gamepad, 0, out ulong systemToken);
manager.RegisterKeyboardLayoutCallback(keyboard, out ulong layoutToken);
```

---

## Advanced: dispatcher wait handle, reading device/axis/button/switch/raw

**Dispatcher wait handle** — block a thread on the dispatcher instead of polling `DispatchCallbacks`:

```csharp
using (var waitHandle = manager.CreateDispatcherWaitHandle())
{
    if (waitHandle != null)
    {
        // e.g. on a background thread: when signalled, run the dispatcher
        waitHandle.SafeWaitHandle.WaitOne();
        manager.DispatchCallbacks(1000);
    }
}
```

**Typed state and device/raw report from a reading**

In a reading callback you can get **typed state** directly from the reading (no polling): `GetGamepadStateFromReading(args.Reading)`, `GetMouseStateFromReading(args.Reading)`, `GetKeyboardStateFromReading(args.Reading, maxKeys)`, `GetSensorsStateFromReading`, `GetArcadeStickStateFromReading`, `GetFlightStickStateFromReading`, `GetRacingWheelStateFromReading`. Or get the device pointer or raw report:

```csharp
manager.ReadingCallback += (s, args) =>
{
    using (args.Reading)
    {
        GamepadState? state = manager.GetGamepadStateFromReading(args.Reading);
        // Or: IntPtr devicePtr = manager.GetDeviceFromReading(args.Reading); (caller must release)
        // Or: manager.GetRawReportFromReading(args.Reading, out IntPtr reportPtr); (valid only for reading lifetime)
    }
};
```

**Generic controller axis/button/switch state**

For readings that expose generic controller state (any device type):

```csharp
uint axisCount = manager.GetControllerAxisCount(args.Reading);
var axisValues = new float[axisCount];
uint written = manager.GetControllerAxisState(args.Reading, axisValues);

uint buttonCount = manager.GetControllerButtonCount(args.Reading);
var buttonValues = new uint[buttonCount];
manager.GetControllerButtonState(args.Reading, buttonValues);

uint switchCount = manager.GetControllerSwitchCount(args.Reading);
var switchPositions = new int[switchCount];
manager.GetControllerSwitchState(args.Reading, switchPositions);
```

---

## Low-level device APIs (extra axes/buttons, DirectInput, raw reports)

On any device (gamepad, keyboard, mouse) you get full access to:

**Extra axis/button indexes** (for controllers that expose more than the standard set):

```csharp
uint axisCount = gamepad.GetExtraAxisCount(GameInputKinds.Gamepad);
uint[] axisIndexes = gamepad.GetExtraAxisIndexes(GameInputKinds.Gamepad);
uint buttonCount = gamepad.GetExtraButtonCount(GameInputKinds.Gamepad);
uint[] buttonIndexes = gamepad.GetExtraButtonIndexes(GameInputKinds.Gamepad);
```

**DirectInput escape** (send custom commands to the device):

```csharp
var (success, bytesWritten) = gamepad.DirectInputEscape(command: 1, bufferIn: null, bufferOut: new byte[64]);
```

**Input mapper** (query how axes/buttons are mapped; dispose when done):

```csharp
using (InputMapper? mapper = gamepad.CreateInputMapper())
{
    if (mapper != null)
    {
        AxisMappingInfo? leftStickX = mapper.GetGamepadAxisMappingInfo((int)GameInputGamepadAxes.LeftThumbstickX);
        ButtonMappingInfo? buttonA = mapper.GetGamepadButtonMappingInfo((int)GameInputGamepadButtons.A);
    }
}
```

All axis/button constants: see [MAPPING_REFERENCE.md](MAPPING_REFERENCE.md) (gamepad, flight stick, racing wheel, arcade stick).

**Full device info** (vendor ID, product ID, firmware version, capabilities):

```csharp
DeviceInfo? info = gamepad.GetDeviceInfo();
if (info != null)
{
    ushort vid = info.VendorId, pid = info.ProductId;
    string name = info.DisplayName;
    uint motorCount = info.ForceFeedbackMotorCount;
}
```

**Raw device reports** (create and send HID-style reports; get/set report data):

```csharp
using var report = gamepad.CreateRawDeviceReport(reportId: 0, reportKind: 0);
if (report != null)
{
    if (report.GetReportInfo(out var info))
        ; // info.Kind, info.Id, info.Size
    uint size = report.GetRawDataSize();
    var buffer = new byte[size];
    uint read = report.GetRawData(buffer);
    report.SetRawData(buffer);  // write back if needed
    gamepad.SendRawDeviceOutput(report);  // report is not disposed by this call
}
// Optional: IntPtr overload and Marshal.Release for low-level use
```

---

## Optional: logging (diagnostics)

You can pass an `ILogger` into the manager for diagnostics (e.g. when devices fail to wrap or GameInput is unavailable).

```csharp
using Microsoft.Extensions.Logging;

ILogger logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<GameInputManager>();
using var manager = new GameInputManager(logger);
```

---

## Disposal and lifecycle

- **GameInputManager:** Implements `IDisposable`. Use `using var manager = new GameInputManager();` or call `Dispose()` when done so native callbacks and the GameInput instance are released.
- **Devices:** GamepadDevice, KeyboardDevice, MouseDevice implement `IDisposable`. If you keep references beyond the manager’s scope, dispose them when no longer needed.

```csharp
using (var manager = new GameInputManager())
{
    var devices = manager.GetDevices();
    // ... use devices; manager.Dispose() releases native refs
}
```

### Thread safety and callback rules

- **Not thread-safe:** Use the manager from a **single thread** (e.g. game loop) or synchronize access externally. Call `DispatchCallbacks` only from that thread.
- **Do not call `Dispose()` from inside a callback:** Do not dispose the manager (or devices) from a `DeviceCallback` or `ReadingCallback` handler. Unregister or shut down on your main loop instead.
- **Do not call `UnregisterCallback` from inside that same callback:** The native API does not allow unregistering a callback from within its own handler. Stop or unregister from your main loop after the callback returns.
- **Device ownership:** You own device wrappers returned from `GetDevices()`. Dispose them when done, or scope use to the manager’s lifetime. Each call to `GetDevices()` returns **new** wrapper instances; use `DeviceId` to correlate or call `TryGetDeviceByDeviceId(deviceId)` to retrieve a device by ID.
- **Return values:** Check return values where provided (e.g. `SendRawDeviceOutput` returns `bool`). Use `GameInputManager(logger)` when debugging to see when interop fails (e.g. device disconnected).

### Security and safety

- **Callback lifetime:** On `Dispose()`, the wrapper unregisters all callbacks and then frees the context handles. Do not call `Dispose` or `UnregisterCallback` from inside a callback handler.
- **DLL loading:** By default, the GameInput DLL is loaded from System32 first, then the application directory, then the default search path. For maximum security (e.g. to reduce DLL hijacking risk), create the manager with **load-only-from-System32**: `new GameInputManager(logger, loadOnlyFromSystem32: true)`. The DLL is then loaded only from System32; if it is not there, init fails and `GetDevices()` returns an empty list. See [SECURITY.md](SECURITY.md).

---

## Samples and compatibility

- **Samples (Core repo):** `samples/GameInputSharp.Samples.Console` (enumeration and rumble), `samples/GameInputSharp.Samples.MonoGame` (game loop integration).
- **Engines and desktop:** See [COMPATIBILITY.md](COMPATIBILITY.md) for Unity, MonoGame, Godot, Stride, and WPF/WinUI.

For full API details, see the XML documentation on the types in your IDE or in the packaged assembly.
