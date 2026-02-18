# GameInputSharp.Samples.Stride

Example of using GameInputSharp.Core from a **Stride** (formerly Xenko) game or script on Windows.

## Requirements

- Stride with .NET 8
- Windows (GameInput is Windows-only)
- GameInputSharp.Core as a project or package reference
- GameInput runtime on the machine

## Setup

1. Add a reference to **GameInputSharp.Core** (and ensure Microsoft.GameInput is available) in your Stride game project.
2. Use the same APIs as in the console sample; scripts run on the same CLR.

## Example script

Create a sync or script component that holds a `GameInputManager` and polls in update:

```csharp
using Stride.Engine;
using Stride.Core;
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

public class GameInputStrideScript : SyncScript
{
    private GameInputManager _manager;

    public override void Start()
    {
        _manager = new GameInputManager();
    }

    public override void Update()
    {
        foreach (var device in _manager.GetDevices())
        {
            if (device is GamepadDevice gamepad)
                gamepad.Haptics.SetVibration(0.4f, 0.4f); // example rumble
        }
    }

    public override void Cancel()
    {
        _manager?.Dispose();
    }
}
```

For current gamepad state use `_manager.GetCurrentGamepadState(gamepad)`; see the [Full usage guide](../../docs/USAGE.md).

## Platform

Windows-only. On other platforms, `GetDevices()` returns an empty list.
