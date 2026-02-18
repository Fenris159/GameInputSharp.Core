# GameInputSharp.Samples.MonoGame

Minimal MonoGame (Windows) sample that integrates GameInputSharp.Core in the game loop.

## Requirements

- .NET 8, Windows
- [MonoGame.Framework.WindowsDX](https://www.nuget.org/packages/MonoGame.Framework.WindowsDX) (referenced in the project)
- GameInput runtime (GameInput.dll / GameInputRedist.dll) on the machine

## Run

From the repository root:

```bash
dotnet run --project samples/GameInputSharp.Samples.MonoGame/GameInputSharp.Samples.MonoGame.csproj
```

## What it does

- **Initialize:** Creates a `GameInputManager` in `Initialize()`.
- **Update:** Each frame, calls `GetDevices()` and iterates gamepads (example placeholder for rumble).
- **UnloadContent:** Disposes the manager.

## Key code

```csharp
protected override void Initialize()
{
    _gameInputManager = new GameInputManager();
    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    if (_gameInputManager != null)
    {
        var devices = _gameInputManager.GetDevices();
        foreach (var d in devices)
        {
            if (d is GamepadDevice gamepad && gamepad.IsConnected)
                gamepad.Haptics.SetVibration(0.5f, 0.5f); // example rumble
        }
    }
    base.Update(gameTime);
}

protected override void UnloadContent()
{
    _gameInputManager?.Dispose();
    base.UnloadContent();
}
```

For full usage (polling state, callbacks), see the [Full usage guide](../../docs/USAGE.md) and [Compatibility](../../docs/COMPATIBILITY.md).
