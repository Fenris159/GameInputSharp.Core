# GameInputSharp.Core — test results

This document describes the automated tests for the wrapper, how to run them, and the latest simulated run results.

---

## How to run

From the repository root:

```bash
dotnet test tests/GameInputSharp.Tests/GameInputSharp.Tests.csproj
```

With verbose output:

```bash
dotnet test tests/GameInputSharp.Tests/GameInputSharp.Tests.csproj --verbosity normal
```

**Requirements:** .NET 8 SDK; Windows (target is `net8.0-windows`). The GameInput runtime (gameinput.dll / GameInputRedist) is **not** required for most tests. When the runtime is missing, `GetDevices()` returns an empty list and tests that depend on it still pass.

---

## What is tested

| Area | Tests | Hardware / runtime |
|------|--------|---------------------|
| **Manager lifecycle** | Dispose idempotent; GetDevices/GetCurrentTimestamp/GetReadingTimestamp/FindDeviceFromId/FindDeviceFromPlatformString/RegisterReadingCallback throw after dispose | None |
| **GetDevices** | Returns non-null list; multiple calls OK; empty list when runtime missing | Runtime optional (empty list if missing) |
| **TryGetDeviceByDeviceId** | Null/empty → false; non-existent ID → false; after dispose throws; when devices exist, matching ID returns device | One test asserts when devices exist (skips if none) |
| **PlayForceFeedbackConstant** | When a gamepad is connected, call PlayForceFeedbackConstant(0, 1000, 0.5f) does not throw | Skips if no devices (same as other gamepad tests) |
| **Null/guard behavior** | GetCurrentGamepadState(null), GetCurrentMouseState(null), FindDeviceFromId(null/short), FindDeviceFromPlatformString(null/empty), RegisterReadingCallback(null) → null or false | None |
| **GetReadingTimestamp** | Null reading → 0; after dispose throws | None |
| **Constructor** | With null logger and with NullLogger.Instance does not throw | None |
| **Constants / contract** | `GameInputGamepadButtons` and `GameInputGamepadAxes` expected bit values; `GamepadState.Buttons` testable with enum; `IInputDevice` DisplayName/DeviceId non-null for enumerated devices | Runtime optional (contract test runs over empty list if no devices) |
| **Haptics** | `HapticLocation` enum values | None |

No tests require a physical gamepad, keyboard, or mouse to be connected. Tests that validate behavior when devices are present (e.g. `TryGetDeviceByDeviceId_WhenDeviceExists_ReturnsTrueAndDevice`) simply skip their assertion when `GetDevices()` returns an empty list.

---

## Latest run results

**Run date:** 2026-02-17 (Windows).

| Result | Count |
|--------|--------|
| **Passed** | 30 |
| **Failed** | 0 |
| **Total** | 30 |
| **Time** | ~1 s |

### Test list (all passed)

- **GameInputManagerTests:** GetDevices_ReturnsList_DoesNotThrow, GetDevices_AfterDispose_Throws, GetDevices_CalledMultipleTimes_DoesNotThrow, Dispose_IsIdempotent_DoesNotThrow, GetCurrentTimestamp_AfterDispose_Throws, GetCurrentTimestamp_WhenNotDisposed_ReturnsValue, TryGetDeviceByDeviceId_NullOrEmpty_ReturnsFalseAndNullDevice, TryGetDeviceByDeviceId_AfterDispose_Throws, TryGetDeviceByDeviceId_NonExistentId_ReturnsFalse, TryGetDeviceByDeviceId_WhenDeviceExists_ReturnsTrueAndDevice, **Gamepad_WhenConnected_GetCurrentGamepadState_ReturnsNonNull**, **Gamepad_WhenConnected_PlayForceFeedbackConstant_ReturnsBool**, GetCurrentGamepadState_NullGamepad_ReturnsNull, GetCurrentGamepadState_AfterDispose_Throws, GetCurrentMouseState_NullMouse_ReturnsNull, GetReadingTimestamp_NullReading_ReturnsZero, GetReadingTimestamp_AfterDispose_Throws, FindDeviceFromId_NullOrShortArray_ReturnsNull, FindDeviceFromId_AfterDispose_Throws, FindDeviceFromPlatformString_NullOrEmpty_ReturnsNull, FindDeviceFromPlatformString_AfterDispose_Throws, RegisterReadingCallback_NullDevice_ReturnsFalse, RegisterReadingCallback_AfterDispose_Throws, Constructor_WithNullLogger_DoesNotThrow, Constructor_WithLogger_DoesNotThrow
- **DeviceWrapperTests:** HapticLocation_Enum_HasExpectedValues, IInputDevice_Contract_DisplayNameAndDeviceId_AreStrings, GameInputGamepadButtons_HasExpectedBitValues, GamepadState_Buttons_CanBeTestedWithGameInputGamepadButtons, GameInputGamepadAxes_HasExpectedValues

---

## Notes

- **No mocking of native APIs:** Tests call the real wrapper; when the GameInput runtime is absent, init fails gracefully and APIs return empty list / null / false as documented.
- **CI:** Run `dotnet test` on Windows (e.g. GitHub Actions `windows-latest`) to validate the wrapper without requiring attached input devices.
- For manual testing with a gamepad (polling, callbacks, rumble), use the samples in `samples/` (e.g. GameInputSharp.Samples.Console).

---

## Testing with a connected gamepad

With a gamepad connected and the GameInput runtime available:

1. **Run the full test suite** — The tests `Gamepad_WhenConnected_GetCurrentGamepadState_ReturnsNonNull` and `TryGetDeviceByDeviceId_WhenDeviceExists_ReturnsTrueAndDevice` only assert when at least one device is present; they validate live state and device lookup.
2. **Run the Console sample** — It enumerates devices, prints gamepad state (buttons, triggers, thumbsticks), verifies `TryGetDeviceByDeviceId(DeviceId)`, and triggers a short rumble:

   ```bash
   dotnet run --project samples/GameInputSharp.Samples.Console/GameInputSharp.Samples.Console.csproj
   ```

   Example output when a gamepad is found:

   ```
   GameInputSharp.Samples.Console — device enumeration and gamepad test.
   Devices found: 1
     - <Your Gamepad Name> (Id: ...)
       [Gamepad] Buttons: 0x...  L/R Trigger: 0.00 / 0.00
       [Gamepad] LeftStick: (0.00, 0.00)  RightStick: (0.00, 0.00)
       [Gamepad] TryGetDeviceByDeviceId(DeviceId): True
       [Gamepad] Triggering short rumble.
   Done.
   ```

If you see "Devices found: 0", ensure the [GameInput runtime](https://learn.microsoft.com/en-us/gaming/gdk/_content/gc/reference/input/gameinput-overview) is installed and the gamepad is connected and recognized by Windows.
