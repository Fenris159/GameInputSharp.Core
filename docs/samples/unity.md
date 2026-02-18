# Samples — Unity

Use GameInputSharp.Core in a Unity project by referencing the library and calling it from C# scripts. The sample content lives in **`samples/GameInputSharp.Samples.Unity`** (README and integration notes).

## Setup

1. **Reference the library** — Build GameInputSharp.Core and copy `GameInputSharp.Core.dll` (and dependencies) into `Assets/Plugins`, or reference the project if your Unity version supports it. Prefer .NET 8 when possible (Unity 6+).
2. **Add Microsoft.GameInput** — Use NuGetForUnity or copy the package contents. The GameInput runtime must be available on Windows.
3. **Redistribute** — Bundle the GameInput runtime with your build per Microsoft’s terms.

## Example: MonoBehaviour

Create a `GameInputManager` in `Start` and poll or react in `Update`. Dispose in `OnDestroy`.

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

## Polling state

Use the same APIs as in the [Full usage guide](../USAGE.md): `GetCurrentGamepadState(gamepad)`, `GetCurrentMouseState(mouse)`, `GetCurrentKeyboardState(keyboard, maxKeys)`.

## Input System (optional)

To integrate with Unity’s Input System, implement a custom provider or push GameInput state into Unity’s APIs. See the sample README for notes.

## Platform

GameInput is Windows-only. On other platforms, `GetDevices()` returns an empty list.

---

**Sample location:** `samples/GameInputSharp.Samples.Unity/README.md`
