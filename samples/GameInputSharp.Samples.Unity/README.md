# GameInputSharp.Core Unity integration

Use GameInputSharp.Core in a Unity project by referencing the GameInputSharp.Core library (or the project) and calling it from C# scripts.

## Setup

1. **Copy or reference the library**
   - Build GameInputSharp.Core and copy `GameInputSharp.Core.dll` (and dependencies) into your Unity project `Assets/Plugins`, or
   - Use a Unity-compatible .NET version: GameInputSharp.Core targets net8.0; Unity 6+ supports .NET 8. Ensure your Unity project is set to .NET 8.

2. **Add Microsoft.GameInput**
   - Add the [Microsoft.GameInput NuGet](https://www.nuget.org/packages/Microsoft.GameInput) package (e.g. via NuGetForUnity or by copying the package contents). Unity must have the native GameInput runtime available on Windows.

3. **Redistribute gameinput.dll**
   - Bundle the GameInput runtime with your build per Microsoft’s redistribution terms.

## Usage

In any MonoBehaviour, create a `GameInputManager` (e.g. in `Awake` or `Start`) and call `GetDevices()` each frame or when needed. Use `GamepadDevice.Haptics.SetVibration(left, right)` for rumble.

```csharp
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

public class GameInputProvider : MonoBehaviour
{
    private GameInputManager _manager;

    void Start()
    {
        _manager = new GameInputManager();
    }

    void Update()
    {
        foreach (var d in _manager.GetDevices())
        {
            if (d is GamepadDevice gamepad)
                gamepad.Haptics.SetVibration(0.5f, 0.5f); // example
        }
    }

    void OnDestroy() => _manager?.Dispose();
}
```

## Input System override (optional)

To integrate with Unity’s Input System, implement a custom `InputProvider` or use GameInputSharp as a backing source and push state into Unity’s input APIs. This is left as an integration exercise.

## Platform

GameInput is Windows-only. On other platforms, `GetDevices()` will return an empty list.
