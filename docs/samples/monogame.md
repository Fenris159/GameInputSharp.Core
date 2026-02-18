# Samples — MonoGame

Minimal MonoGame (Windows) sample that integrates GameInputSharp in the game loop. The runnable project is in **`samples/GameInputSharp.Samples.MonoGame`**.

## Run

From the repository root:

```bash
dotnet run --project samples/GameInputSharp.Samples.MonoGame/GameInputSharp.Samples.MonoGame.csproj
```

## Requirements

- .NET 8, Windows
- [MonoGame.Framework.WindowsDX](https://www.nuget.org/packages/MonoGame.Framework.WindowsDX)
- GameInput runtime on the machine

## Pattern

- **Initialize:** Create `GameInputManager` in `Initialize()`.
- **Update:** Each frame, call `GetDevices()` and use gamepads (e.g. rumble).
- **UnloadContent:** Dispose the manager.

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
                gamepad.Haptics.SetVibration(0.5f, 0.5f);
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

For polling current state and callbacks, see the [Full usage guide](../USAGE.md).

---

**Sample location:** `samples/GameInputSharp.Samples.MonoGame/`
