# GameInputSharp.Core

Universal, idiomatic C# wrapper for **Microsoft.GameInput**. Targets .NET 8+ with optional shims for Unity, MonoGame, Godot C#, Stride, and WPF/WinUI. This package covers game and desktop input, haptics, and device callbacks.

Package: [GameInputSharp.Core on NuGet.org](https://www.nuget.org/packages/GameInputSharp.Core/)

Repository: [github.com/Fenris159/GameInputSharp.Core](https://github.com/Fenris159/GameInputSharp.Core)

## Compliance and redistribution

**This is a third-party C# wrapper library.** It requires the official **Microsoft.GameInput** NuGet package. Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

- **Dependency:** Use only the [Microsoft.GameInput NuGet package](https://www.nuget.org/packages/Microsoft.GameInput). Do not depend on the full GDK or Xbox-specific libraries.
- **Redistribution:** You must redistribute **gameinput.dll** and any other runtime files according to Microsoft's rules. See the Microsoft.GameInput package and documentation for current redistribution requirements.
- **Runtime:** The Microsoft.GameInput NuGet package does not ship the runtime DLL. The GameInput runtime must be available on the target Windows machine.

## Setup

Add the package:

```powershell
dotnet add package GameInputSharp.Core
```

Or add a package reference:

```xml
<PackageReference Include="GameInputSharp.Core" Version="1.0.1" />
```

Target **.NET 8+**. Full device support requires Windows and the GameInput runtime.

## Quick start

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

using var manager = new GameInputManager();
var devices = manager.GetDevices();
foreach (var device in devices)
{
    Console.WriteLine($"{device.DisplayName} - {device.DeviceId}");
    if (device is GamepadDevice gamepad)
        gamepad.Haptics.SetVibration(0.5f, 0.5f);
}
```

## Included docs

The package includes offline documentation under `docs-site/` when built with MkDocs before packing, plus markdown guides under `docs/`.

- Usage guide: `docs/USAGE.md`
- Compatibility guide: `docs/COMPATIBILITY.md`
- Changelog: `CHANGELOG.md`
- Full documentation and samples: [GitHub repository](https://github.com/Fenris159/GameInputSharp.Core)

## License

MIT. By using this library you agree to comply with Microsoft's redistribution and licensing terms for GameInput.
