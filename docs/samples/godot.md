# Samples — Godot C#

Example of using GameInputSharp.Core from a **Godot 4.x C#** project on Windows. Documented example and setup are in **`samples/GameInputSharp.Samples.Godot`**.

## Requirements

- Godot 4.2+ with .NET 8
- Windows (GameInput is Windows-only)
- GameInputSharp.Core and Microsoft.GameInput referenced in the Godot C# project
- GameInput runtime on the build machine

## Setup

1. Build GameInputSharp.Core and copy the DLL (and dependencies) into your Godot project.
2. Add a reference to the Core assembly in your Godot C# project.
3. Ensure the GameInput native runtime is present where the game runs.

## Example script

Attach to a node; create the manager in `_Ready`, poll in `_Process`, dispose in `_ExitTree`.

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
                gamepad.Haptics.SetVibration(0.3f, 0.3f);
        }
    }

    public override void _ExitTree()
    {
        _manager?.Dispose();
    }
}
```

Use `_manager.GetCurrentGamepadState(gamepad)` and similar for per-frame state; see the [Full usage guide](../USAGE.md).

---

**Sample location:** `samples/GameInputSharp.Samples.Godot/README.md`
