# Quick start

Get GameInputSharp.Core running in a few minutes: add packages, create a manager, enumerate devices, and optionally poll or rumble.

---

## 1. Create a project and add packages

Create a **console app** (or any .NET 8+ Windows app):

```bash
dotnet new console -n MyGameInputApp -f net8.0
cd MyGameInputApp
```

Add the wrapper and the official Microsoft dependency:

```bash
dotnet add package GameInputSharp.Core
dotnet add package Microsoft.GameInput
```

Or in the `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="GameInputSharp.Core" Version="1.0.1" />
  <PackageReference Include="Microsoft.GameInput" Version="3.4.218" />
</ItemGroup>
```

Target **.NET 8+** and run on **Windows** for device enumeration. Other targets build but return no devices.

---

## 2. Enumerate devices

Replace `Program.cs` (or add this to your game loop):

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

using var manager = new GameInputManager();
var devices = manager.GetDevices();

Console.WriteLine($"Found {devices.Count} device(s)");
foreach (var device in devices)
{
    Console.WriteLine($"  {device.DisplayName} — {device.DeviceId}");
    if (device is GamepadDevice)
        Console.WriteLine("    -> Gamepad");
    else if (device is KeyboardDevice)
        Console.WriteLine("    -> Keyboard");
    else if (device is MouseDevice)
        Console.WriteLine("    -> Mouse");
}
```

Run on a machine with the **GameInput runtime** installed (e.g. Windows with GameInput inbox, or `winget install Microsoft.GameInput`). If the runtime is missing, `GetDevices()` returns an empty list and does **not** throw.

---

## 3. Poll gamepad state (one frame)

```csharp
var gamepad = manager.GetDevices().OfType<GamepadDevice>().FirstOrDefault();
if (gamepad == null) return;

GamepadState? state = manager.GetCurrentGamepadState(gamepad);
if (state == null) return;

float leftTrigger  = state.Value.LeftTrigger;   // 0–1
float rightTrigger = state.Value.RightTrigger;
float leftX        = state.Value.LeftThumbstickX; // -1–1
float leftY        = state.Value.LeftThumbstickY;
uint buttons      = state.Value.Buttons;         // bitmask; use GameInputGamepadButtons
```

Use the **game loop**: call `GetCurrentGamepadState(gamepad)` (and similar for mouse/keyboard) every frame. See [Full usage guide](USAGE.md) for mouse and keyboard polling.

---

## 4. Simple rumble

```csharp
if (gamepad != null)
{
    gamepad.Haptics.SetVibration(0.5f, 0.5f);  // left (low-freq), right (high-freq), 0–1
    // ... later ...
    gamepad.Haptics.SetVibration(0f, 0f);      // stop
}
```

---

## 5. Device connect/disconnect (callbacks)

Subscribe to `DeviceCallback` and call `DispatchCallbacks` from your game loop:

```csharp
manager.DeviceCallback += (sender, args) =>
{
    bool connected = (args.CurrentStatus & 0x1) != 0; // GameInputDeviceStatus.Connected
    Console.WriteLine($"Device {args.DeviceId}: {(connected ? "Connected" : "Disconnected")}");
};

// Each frame:
manager.DispatchCallbacks(quotaMicroseconds: 1000);
```

---

## 6. Disposal

Always dispose the manager (and any device references you keep) so native resources are released:

```csharp
using var manager = new GameInputManager();
// ... use manager and devices ...
// Dispose automatically when leaving scope, or call manager.Dispose();
```

Do **not** call `Dispose()` or `UnregisterCallback` from inside a callback handler. See [Full usage guide — Disposal and lifecycle](USAGE.md#disposal-and-lifecycle).

---

## No devices?

- **Init OK but 0 devices:** Controller may be exposed only via XInput/Windows.Gaming.Input; try connecting over USB. See [Compatibility](COMPATIBILITY.md).
- **Init failed / DLL not loaded:** Install the GameInput runtime or Visual C++ Redistributable (x64). See [Troubleshooting](troubleshooting.md).

Next: [Full usage guide](USAGE.md) for installation details, polling, advanced haptics, reading callbacks, and all APIs.
