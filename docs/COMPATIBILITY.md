# GameInputSharp.Core — engine and app compatibility

GameInputSharp.Core is a plain .NET library with no engine-specific code.

---

## Repository and build output

Open and build from **this repository root** (the folder that contains `GameInputSharp.Core.sln`). All build artifacts (`bin`, `obj`) and `dotnet pack` output are written under that folder. If you moved the project from another path, remove or rename the old folder so tooling and builds use only this copy. This repo has its own `.git` so Source Link and Git use this directory as the root. You can use it from any C# environment that can load a .NET 8+ assembly and run on Windows.

---

## Unity

- **Target:** .NET Standard 2.1 or .NET 8 (Unity 6+). Prefer .NET 8 when possible.
- **How:** Reference **GameInputSharp.Core** (the library output, e.g. `GameInputSharp.Core.dll`) in `Assets/Plugins` (or the project output). Add the Microsoft.GameInput NuGet (e.g. NuGetForUnity). Ensure the GameInput runtime is available on Windows.
- **Shim:** See `samples/GameInputSharp.Samples.Unity/README.md` for a minimal integration pattern and optional Input System override notes.

---

## MonoGame

- **Target:** net8.0-windows (or net6.0-windows). Use the Windows DX or Desktop project type.
- **How:** Add a project reference to **GameInputSharp.Core**. In `Game.Initialize()` create a `GameInputManager`; in `Update()` call `GetDevices()` and use `GamepadDevice.Haptics` for rumble.
- **Sample:** `samples/GameInputSharp.Samples.MonoGame` shows a minimal game loop with GameInputSharp.

---

## Godot C\#

- **Target:** .NET 8 (Godot 4.2+). Use a Windows export.
- **How:** Reference **GameInputSharp.Core** from your Godot C# project. Call from `_Process` or `_Input` as needed. No special shim; use the same APIs as in the console sample. Ensure the GameInput runtime is present on the Windows build.

---

## Stride

- **Target:** .NET 8. Stride scripts run on the same CLR; reference GameInputSharp and use from a sync or script component.
- **How:** Add **GameInputSharp.Core** as a reference, create `GameInputManager` in script start, poll in update. Rumble via `GamepadDevice.Haptics.SetVibration`. Windows-only.

---

## WPF / WinUI desktop

- **Target:** net8.0-windows (or net9.0), WPF/WinUI app.
- **How:** Add a reference to **GameInputSharp.Core**. Use `GameInputManager` and `GetDevices()` from the UI thread or a background thread; marshal results to the UI as needed. For rumble, use `GamepadDevice.Haptics`. No extra shim required.

---

## Summary

| Environment | Reference GameInputSharp.Core | Sample | Documentation |
|-------------|------------------------------|--------|----------------|
| Unity       | DLL in Plugins or project    | `samples/GameInputSharp.Samples.Unity` | [Samples — Unity](samples/unity.md) |
| MonoGame    | Project reference             | `samples/GameInputSharp.Samples.MonoGame` | [Samples — MonoGame](samples/monogame.md) |
| Godot C#    | Project reference             | `samples/GameInputSharp.Samples.Godot` | [Samples — Godot](samples/godot.md) |
| Stride      | Project reference             | `samples/GameInputSharp.Samples.Stride` | [Samples — Stride](samples/stride.md) |
| WPF / WinUI | Project reference             | `samples/GameInputSharp.Samples.Wpf` | [Samples — WPF / WinUI](samples/wpf-winui.md) |

All require **Windows** and the **GameInput runtime** (gameinput.dll / GameInputRedist.dll) for devices to be enumerated. For a single index of all samples, see [Samples by environment](samples/index.md).

---

## Controllers and connection

- **Xbox Elite / wireless adapter:** Some Xbox controllers connected via the **Xbox Wireless Adapter** are exposed to Windows as XInput or through the Xbox Accessories stack; the **GameInput** API may not enumerate them on all Windows/driver configurations. If the Console sample shows **Init: OK, Raw devices: 0**, the runtime is fine but no devices are being reported by GameInput. Try connecting the same controller over **USB** to see if it appears; if it does, the wireless adapter path may not be wired into GameInput on your system. The Xbox Accessories app can still see the device via a different API.
