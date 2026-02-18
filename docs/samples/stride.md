# Samples — Stride

Example of using GameInputSharp.Core from a **Stride** game or script on Windows. Documented example is in **`samples/GameInputSharp.Samples.Stride`**.

## Requirements

- Stride with .NET 8
- Windows (GameInput is Windows-only)
- GameInputSharp.Core as a project or package reference
- GameInput runtime on the machine

## Setup

Add a reference to GameInputSharp.Core (and Microsoft.GameInput) in your Stride game project. Scripts run on the same CLR; use the same APIs as in the console sample.

## Example script

Use a sync or script component: create the manager in `Start`, poll in `Update`, dispose in `Cancel`.

```csharp
using Stride.Engine;
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
                gamepad.Haptics.SetVibration(0.4f, 0.4f);
        }
    }

    public override void Cancel()
    {
        _manager?.Dispose();
    }
}
```

For current gamepad state use `_manager.GetCurrentGamepadState(gamepad)`; see the [Full usage guide](../USAGE.md).

---

**Sample location:** `samples/GameInputSharp.Samples.Stride/README.md`
