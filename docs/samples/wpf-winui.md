# Samples — WPF / WinUI

Minimal WPF sample that lists GameInput devices and refreshes on demand. The runnable project is in **`samples/GameInputSharp.Samples.Wpf`**.

## Run

From the repository root:

```bash
dotnet run --project samples/GameInputSharp.Samples.Wpf/GameInputSharp.Samples.Wpf.csproj
```

## Requirements

- .NET 8, Windows
- GameInput runtime on the machine

## What it does

- **On load:** Creates a `GameInputManager` and populates a list with `GetDevices()`.
- **Refresh button:** Re-enumerates and updates the list.
- **On close:** Disposes the manager.

## Pattern

Use the manager on the UI thread (or marshal from a background thread with `Dispatcher.Invoke`).

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

For callbacks or polling on a background thread, marshal results to the UI. See the [Full usage guide](../USAGE.md) and [Compatibility](../COMPATIBILITY.md).

---

**Sample location:** `samples/GameInputSharp.Samples.Wpf/`
