# GameInputSharp.Core

Universal, idiomatic C# wrapper for **Microsoft.GameInput**. Targets .NET 8+ with optional shims for Unity, MonoGame, Godot C#, Stride, and WPF/WinUI. This is the **open-core** package: game and desktop input, haptics, and device callbacks.

For **medical, simulation, and robotics** extensions (multi-device haptic aggregation, calibration, audit logging, network latency compensation, ROS2 stubs), use the separate **GameInputSharp.Enterprise** package, which depends on this package.

**Repository layout:** This repo contains the **GameInputSharp.Core** project only (open source). Open **GameInputSharp.Core.sln** to build the library, samples, and tests. There are no Enterprise-specific files or build output in this directory. The **GameInputSharp.Enterprise** package is a separate, licensed product in its own repository and references this package via NuGet only. **During development** this package is not published to NuGet.org; after release, other developers and Enterprise will rely on NuGet for the dependency.

---

## Compliance and redistribution

**This is a third-party C# wrapper library.** It requires the official **Microsoft.GameInput** NuGet package. Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

- **Dependency:** Use only the [Microsoft.GameInput NuGet package](https://www.nuget.org/packages/Microsoft.GameInput). Do not depend on the full GDK or Xbox-specific libraries.
- **Redistribution:** You must redistribute **gameinput.dll** (and any other runtime files) according to [Microsoft's rules](https://learn.microsoft.com/en-us/gaming/gdk/). See the Microsoft.GameInput package and documentation for current redistribution requirements.
- **How the wrapper and Microsoft.GameInput relate:** The developer adds the official Microsoft NuGet (or gets it automatically when adding GameInputSharp.Core). This repo and the GameInputSharp.Core package do **not** bundle or repackage Microsoft’s files; they only declare a dependency so NuGet pulls Microsoft.GameInput from nuget.org. See [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md).

---

## Documentation

| Audience | Document | Contents |
|----------|----------|----------|
| **Offline docs (one-stop)** | **docs-site/** in NuGet package | Full MkDocs site: quick start, usage, API/mapping, guides, troubleshooting, glossary. Build with `pip install -r requirements-docs.txt && mkdocs build`; then `dotnet pack` includes it. See [docs/BUILD_DOCS_SITE.md](docs/BUILD_DOCS_SITE.md). |
| **Game and desktop** (PC games, WPF/WinUI, kiosks) | [docs/USAGE.md](docs/USAGE.md) | Installation, manager, devices, polling (gamepad/mouse/keyboard), basic and advanced haptics, device callbacks, disposal. |
| **API constants and flags** | [docs/API_REFERENCE.md](docs/API_REFERENCE.md) | Input kinds, focus policy, device status; pointers to force feedback and mapping refs. |
| **Axis/button mapping reference** | [docs/MAPPING_REFERENCE.md](docs/MAPPING_REFERENCE.md) | Library of all device control layouts: gamepad, flight stick, racing wheel, arcade stick (axes, buttons, switches) for use with `InputMapper`. |
| **Engines and desktop setup** | [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md) | Unity, MonoGame, Godot, Stride, WPF/WinUI. |
| **Packaging and Microsoft.GameInput** | [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) | How the wrapper depends on the official NuGet without bundling Microsoft's files. |
| **Changelog (Core only)** | [docs/CHANGELOG.md](docs/CHANGELOG.md) | Version history and notable changes for GameInputSharp.Core. |
| **Security and safety** | [docs/SECURITY.md](docs/SECURITY.md) | Implemented mitigations (DLL load, buffers, callbacks). |

---

## Quick start

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

using var manager = new GameInputManager();
var devices = manager.GetDevices();
foreach (var device in devices)
{
    Console.WriteLine($"{device.DisplayName} — {device.DeviceId}");
    if (device is GamepadDevice gamepad)
        gamepad.Haptics.SetVibration(0.5f, 0.5f); // simple rumble
}
```

---

## Setup

1. Add **GameInputSharp.Core** and **Microsoft.GameInput** NuGet packages to your project. The **Microsoft.GameInput** reference is required (we wrap that API) and must be present.
2. **NuGet vs runtime:** The Microsoft.GameInput NuGet supplies C++ headers and a link library for native builds; it does **not** ship the runtime DLL (GameInput.dll / GameInputRedist.dll). The wrapper loads the native DLL at runtime from: (1) the application directory, then (2) `%SystemRoot%\System32`. The runtime must be installed on the machine (e.g. inbox on newer Windows, or `winget install Microsoft.GameInput`, or the GameInput redist). If the DLL is present but load fails, install [Visual C++ Redistributable (x64)](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist). If no compatible runtime loads, `GetDevices()` returns an empty list without throwing.
3. Target **.NET 8+** (or use the provided Unity/MonoGame shims where applicable). The library is intended for **Windows**; other targets build but return no devices.

---

## Namespaces

- **GameInputSharp.Core** — Low-level interop (COM/P-Invoke, device enumeration, rumble).
- **GameInputSharp.Abstractions** — `GameInputManager`, `IInputDevice`, `IHapticEffect`.
- **GameInputSharp.Devices** — Device factory, gamepad, keyboard, mouse, motion.
- **GameInputSharp.Haptics** — Basic rumble and advanced waveforms (up to 8 locations per device).

---

## Polling vs callbacks

- **Polling:** Call `GetCurrentGamepadState(gamepad)`, `GetCurrentMouseState(mouse)`, or `GetCurrentKeyboardState(keyboard, maxKeys)` each frame. Use when you need continuous state (buttons, axes, keys).
- **Callbacks:** Subscribe to `GameInputManager.DeviceCallback` and call `DispatchCallbacks(quotaMicroseconds)` from your game loop to receive device connect/disconnect events without re-enumerating.

---

## Samples

- **GameInputSharp.Samples.Console** — Device enumeration and basic gamepad rumble; run on Windows with GameInput runtime for full behavior.
- **GameInputSharp.Samples.MonoGame** — MonoGame game loop integration (Update/Draw with GameInputManager).
- **samples/GameInputSharp.Samples.Unity/README.md** — Unity integration notes and optional Input System override.

**Medical / simulation samples** (e.g. **GameInputSharp.Samples.MedicalSim**) are in the **GameInputSharp.Enterprise** package and repository, not in this Core repo.

---

## Compatibility (engines and desktop)

See **[docs/COMPATIBILITY.md](docs/COMPATIBILITY.md)** for Unity, MonoGame, Godot C#, Stride, and WPF/WinUI setup and shims.

---

## License

See [LICENSE](LICENSE) (MIT). By using this library you agree to comply with Microsoft's redistribution and licensing terms for GameInput.
