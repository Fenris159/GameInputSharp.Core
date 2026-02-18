# GameInputSharp.Samples.Godot

Example of using GameInputSharp.Core from a **Godot 4.x C#** project on Windows.

## Requirements

- Godot 4.2+ with .NET 8
- Windows (GameInput is Windows-only)
- GameInputSharp.Core (DLL or project reference) and Microsoft.GameInput
- GameInput runtime on the build machine

## Setup

1. Build GameInputSharp.Core and copy `GameInputSharp.Core.dll` (and its dependencies, including from Microsoft.GameInput) into your Godot project (e.g. under a `Plugins` or `Libs` folder that Godot includes in the C# build).
2. Add a reference to the Core assembly in your Godot C# project (`.csproj` or Godot project settings).
3. Ensure the GameInput native runtime is present where the game runs.

## Example script

Attach this to a node to enumerate devices and optionally rumble a gamepad. Call from `_Process` or `_Input` as needed.

```csharp
using Godot;
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

public partial class GameInputExample : Node
{
    private GameInputManager _manager;

    public override void _Ready()
    {
        _manager = new GameInputManager();
    }

    public override void _Process(double delta)
    {
        foreach (var device in _manager.GetDevices())
        {
            if (device is GamepadDevice gamepad)
                gamepad.Haptics.SetVibration(0.3f, 0.3f); // example
        }
    }

    public override void _ExitTree()
    {
        _manager?.Dispose();
    }
}
```

For polling current state use `_manager.GetCurrentGamepadState(gamepad)` and similar; see the [Full usage guide](../../docs/USAGE.md).

## Platform

GameInput is Windows-only. On other platforms, `GetDevices()` returns an empty list.
