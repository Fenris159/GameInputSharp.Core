# GameInputSharp.Core

**Universal, idiomatic C# wrapper for Microsoft GameInput.** Gamepads, keyboards, mice, advanced haptics, device callbacks, and force feedback — with one place to learn it all.

This site is the **offline documentation** shipped with the GameInputSharp.Core NuGet package. Use it as the single source of truth for developing with the wrapper: no excuse for missing knowledge.

---

## What this wrapper does

- **Device enumeration** — gamepads, keyboards, mice (and flight stick, racing wheel, arcade stick when supported by the runtime).
- **Polling** — current gamepad, mouse, and keyboard state each frame.
- **Haptics** — simple rumble and full force-feedback effects (constant, ramp, periodic, condition).
- **Callbacks** — device connect/disconnect, reading callbacks, system button (e.g. Xbox guide), keyboard layout.
- **Low-level access** — extra axes/buttons, DirectInput escape, raw device reports, input mapper, focus policy.

**Platform:** Windows only. Requires the [GameInput runtime](https://learn.microsoft.com/en-us/gaming/gdk/) (GameInput.dll / GameInputRedist.dll) and the official **Microsoft.GameInput** NuGet package.

---

## Quick start (30 seconds)

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

using var manager = new GameInputManager();
var devices = manager.GetDevices();
foreach (var device in devices)
{
    Console.WriteLine($"{device.DisplayName} — {device.DeviceId}");
    if (device is GamepadDevice gamepad)
        gamepad.Haptics.SetVibration(0.5f, 0.5f); // rumble
}
```

See [Quick start](quickstart.md) for a minimal runnable setup. See [Full usage guide](USAGE.md) for installation, polling, callbacks, and disposal.

---

## Documentation map

| You want to… | Go to |
|--------------|--------|
| Get running in 5 minutes | [Quick start](quickstart.md) |
| Install, poll devices, use haptics, callbacks | [Full usage guide](USAGE.md) |
| Look up input kinds, focus policy, device status | [API constants & flags](API_REFERENCE.md) |
| Map axes/buttons (gamepad, flight stick, racing wheel, arcade) | [Axis & button mapping](MAPPING_REFERENCE.md) |
| Use with Unity, MonoGame, Godot, Stride, WPF | [Compatibility](COMPATIBILITY.md) |
| Understand Microsoft.GameInput vs this package | [Distribution](DISTRIBUTION.md) |
| Understand security (DLL load, buffers, callbacks) | [Security & safety](SECURITY.md) |
| Fix “no devices”, DLL errors, init failures | [Troubleshooting](troubleshooting.md) |
| See what changed between versions | [Changelog](CHANGELOG.md) |
| Look up a term | [Glossary](glossary.md) |

---

## Namespaces

| Namespace | Purpose |
|-----------|---------|
| **GameInputSharp.Abstractions** | `GameInputManager`, `IInputDevice`, `IHapticEffect`, constants |
| **GameInputSharp.Devices** | Device types, factory, gamepad/keyboard/mouse |
| **GameInputSharp.Haptics** | Rumble, waveforms, force-feedback effects |
| **GameInputSharp.Core** | Low-level interop (use when extending the wrapper) |

---

## Samples

- **GameInputSharp.Samples.Console** — Enumeration, gamepad state, rumble, and init diagnostics.
- **GameInputSharp.Samples.MonoGame** — Game loop integration (Update/Draw with `GameInputManager`).
- **samples/GameInputSharp.Samples.Unity** — Unity integration notes and optional Input System override.

---

## Compliance

This is a **third-party** C# wrapper. It requires the official **Microsoft.GameInput** NuGet package. Not affiliated with, endorsed by, or supported by Microsoft Corporation. You must redistribute the GameInput runtime per [Microsoft’s rules](https://learn.microsoft.com/en-us/gaming/gdk/). See [Distribution](DISTRIBUTION.md) and [LICENSE](../LICENSE) (MIT).

---

*Use the **search** (top) or the **navigation** (left) to find any topic. This site is static and works fully offline.*
