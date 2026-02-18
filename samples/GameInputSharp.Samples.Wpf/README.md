# GameInputSharp.Samples.Wpf

Minimal WPF sample that lists GameInput devices and refreshes on demand. Shows using GameInputSharp.Core from the UI thread.

## Requirements

- .NET 8, Windows
- GameInput runtime (GameInput.dll / GameInputRedist.dll) on the machine

## Run

From the repository root:

```bash
dotnet run --project samples/GameInputSharp.Samples.Wpf/GameInputSharp.Samples.Wpf.csproj
```

## What it does

- **On load:** Creates a `GameInputManager` and calls `GetDevices()` to populate a list.
- **Refresh button:** Re-enumerates devices and updates the list.
- **On close:** Disposes the manager.

## Key code

```csharp
private void OnLoaded(object sender, RoutedEventArgs e)
{
    _manager = new GameInputManager();
    RefreshDevices();
}

private void RefreshDevices()
{
    var devices = _manager.GetDevices();
    foreach (var d in devices)
    {
        string type = d switch
        {
            GamepadDevice => "Gamepad",
            KeyboardDevice => "Keyboard",
            MouseDevice => "Mouse",
            _ => "Device"
        };
        DeviceList.Items.Add($"{type}: {d.DisplayName} — {d.DeviceId}");
    }
}
```

For background-thread polling or callbacks, marshal results to the UI thread (e.g. `Dispatcher.Invoke`). See the [Full usage guide](../../docs/USAGE.md) and [Compatibility](../../docs/COMPATIBILITY.md).
