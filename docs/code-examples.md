# Code examples

Copy-paste examples for common tasks. For full context and disposal rules see [Full usage guide](USAGE.md).

---

## Enumerate and log devices

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

using var manager = new GameInputManager();
foreach (var device in manager.GetDevices())
{
    Console.WriteLine($"{device.DisplayName} — {device.DeviceId}");
    if (device is GamepadDevice) Console.WriteLine("  Type: Gamepad");
    else if (device is KeyboardDevice) Console.WriteLine("  Type: Keyboard");
    else if (device is MouseDevice) Console.WriteLine("  Type: Mouse");
}
```

---

## Poll gamepad every frame

```csharp
var gamepad = manager.GetDevices().OfType<GamepadDevice>().FirstOrDefault();
if (gamepad == null) return;

GamepadState? state = manager.GetCurrentGamepadState(gamepad);
if (state == null) return;

float leftTrigger  = state.Value.LeftTrigger;
float rightTrigger = state.Value.RightTrigger;
float leftX        = state.Value.LeftThumbstickX;
float leftY        = state.Value.LeftThumbstickY;
uint buttons      = state.Value.Buttons;

bool aPressed = (buttons & (uint)GameInputGamepadButtons.A) != 0;
bool bPressed = (buttons & (uint)GameInputGamepadButtons.B) != 0;
```

---

## Poll mouse and keyboard

```csharp
var mouse = manager.GetDevices().OfType<MouseDevice>().FirstOrDefault();
MouseState? mouseState = mouse != null ? manager.GetCurrentMouseState(mouse) : null;

var keyboard = manager.GetDevices().OfType<KeyboardDevice>().FirstOrDefault();
KeyState[] keys = keyboard != null ? manager.GetCurrentKeyboardState(keyboard, maxKeys: 32) : Array.Empty<KeyState>();
```

---

## Simple rumble

```csharp
gamepad.Haptics.SetVibration(0.5f, 0.5f);  // left, right 0–1
gamepad.Haptics.SetVibration(0f, 0f);      // stop
```

---

## Device connect/disconnect

```csharp
manager.DeviceCallback += (sender, args) =>
{
    bool connected = (args.CurrentStatus & GameInputDeviceStatus.Connected) != 0;
    Console.WriteLine($"Device {args.DeviceId}: {(connected ? "Connected" : "Disconnected")}");
};
// In game loop:
manager.DispatchCallbacks(1000);
```

---

## Reading callback (per-device, per–input kind)

```csharp
manager.ReadingCallback += (sender, args) =>
{
    using (args.Reading)
    {
        GamepadState? state = manager.GetGamepadStateFromReading(args.Reading);
        if (state.HasValue)
            ; // use state.Value
    }
};
manager.RegisterReadingCallback(gamepad, GameInputKinds.Gamepad);
// In game loop: manager.DispatchCallbacks(1000);
```

---

## Init diagnostics when devices are missing

```csharp
var (initOk, rawCount) = manager.GetDiagnostics();
if (!initOk)
{
    var (mainWin32, mainEx) = manager.GetMainPathLoadFailure();
    var (path1, path2, exists1, exists2, is64) = manager.GetLoadPaths();
    Console.WriteLine($"DLL paths: {path1} (exists: {exists1}), {path2} (exists: {exists2})");
    if (mainWin32 != 0) Console.WriteLine($"Win32: {mainWin32}");
}
```

---

## System32-only load (security)

```csharp
using var manager = new GameInputManager(logger, loadOnlyFromSystem32: true);
// If DLL not in System32, GetDevices() returns empty list
```

---

## Find device by ID and dispose

```csharp
bool found = manager.TryGetDeviceByDeviceId(deviceIdString, out var device);
if (found && device != null)
{
    try { /* use device */ }
    finally { device.Dispose(); }
}
```

---

## Controllable force-feedback effect

```csharp
using var effect = gamepad.CreateForceFeedbackEffect(
    motorIndex: 0,
    durationMicroseconds: 500_000,
    intensity: 0.8f);
if (effect != null)
{
    effect.Start();
    effect.SetGain(0.5f);
    effect.Stop();
}
```

---

## Using and disposal

```csharp
using (var manager = new GameInputManager())
{
    var devices = manager.GetDevices();
    // use devices; manager and native refs released on exit
}
// Do NOT call Dispose or UnregisterCallback from inside a callback
```
