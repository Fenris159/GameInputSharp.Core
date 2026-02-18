# Input mapping reference (axes, buttons, switches)

This page is a **library of all device control layouts** supported by GameInput and exposed in GameInputSharp.Core for use with `InputMapper` (axis/button mapping queries). Values match Microsoft GameInput v3 (GameInput.h).

---

## Element kinds and switch positions

Used in **mapping results** (`AxisMappingInfo.ControllerElementKind`, `ButtonMappingInfo.ControllerElementKind`, `ReferenceDirection`, `SwitchPosition`):

| Type                        | Values                                                                                                                                 |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| **GameInputElementKind**    | `None` (0), `Axis` (1), `Button` (2), `Switch` (3)                                                                                     |
| **GameInputSwitchPosition** | `Center` (0), `Up` (1), `UpRight` (2), `Right` (3), `DownRight` (4), `Down` (5), `DownLeft` (6), `Left` (7), `UpLeft` (8)                 |

---

## Gamepad

**Axes** — use with `InputMapper.GetGamepadAxisMappingInfo(axisElement)`:

| Constant                           | Value       | Description           |
| ---------------------------------- | ----------- | --------------------- |
| GameInputGamepadAxes.None          | 0x00000000  | No axis               |
| GameInputGamepadAxes.LeftTrigger   | 0x00000001  | Left trigger (analog) |
| GameInputGamepadAxes.RightTrigger  | 0x00000002  | Right trigger (analog) |
| GameInputGamepadAxes.LeftThumbstickX  | 0x00000004  | Left stick X         |
| GameInputGamepadAxes.LeftThumbstickY  | 0x00000008  | Left stick Y         |
| GameInputGamepadAxes.RightThumbstickX | 0x00000010  | Right stick X        |
| GameInputGamepadAxes.RightThumbstickY | 0x00000020  | Right stick Y        |

**Buttons** — use with `InputMapper.GetGamepadButtonMappingInfo(buttonElement)`:

| Constant                                        | Value        | Description           |
| ----------------------------------------------- | ------------ | --------------------- |
| GameInputGamepadButtons.None                    | 0            | No button             |
| GameInputGamepadButtons.Menu                    | 0x00000001   | Menu (Xbox guide)     |
| GameInputGamepadButtons.View                    | 0x00000002   | View / Back           |
| GameInputGamepadButtons.A                       | 0x00000004   | A                     |
| GameInputGamepadButtons.B                       | 0x00000008   | B                     |
| GameInputGamepadButtons.X                       | 0x00000010   | X                     |
| GameInputGamepadButtons.Y                       | 0x00000020   | Y                     |
| GameInputGamepadButtons.DPadUp                  | 0x00000040   | D-pad up              |
| GameInputGamepadButtons.DPadDown                | 0x00000080   | D-pad down            |
| GameInputGamepadButtons.DPadLeft                | 0x00000100   | D-pad left            |
| GameInputGamepadButtons.DPadRight               | 0x00000200   | D-pad right           |
| GameInputGamepadButtons.LeftShoulder            | 0x00000400   | Left bumper           |
| GameInputGamepadButtons.RightShoulder           | 0x00000800   | Right bumper          |
| GameInputGamepadButtons.LeftThumbstick          | 0x00001000   | Left stick click      |
| GameInputGamepadButtons.RightThumbstick         | 0x00002000   | Right stick click     |
| GameInputGamepadButtons.C                       | 0x00004000   | C (optional)          |
| GameInputGamepadButtons.Z                       | 0x00008000   | Z (optional)          |
| GameInputGamepadButtons.LeftTriggerButton       | 0x00010000   | Left trigger (digital) |
| GameInputGamepadButtons.RightTriggerButton      | 0x00020000   | Right trigger (digital) |
| GameInputGamepadButtons.LeftThumbstickUp/Down/Left/Right  | 0x00040000 … | Stick as button       |
| GameInputGamepadButtons.RightThumbstickUp/Down/Left/Right | 0x00400000 … | Stick as button       |
| GameInputGamepadButtons.PaddleLeft1/2, PaddleRight1/2     | 0x04000000 … | Elite paddles         |

---

## Flight stick

**Axes** — `InputMapper.GetFlightStickAxisMappingInfo(axisElement)`:

| Constant                          | Value       | Description |
| --------------------------------- | ----------- | ----------- |
| GameInputFlightStickAxes.None     | 0           | No axis     |
| GameInputFlightStickAxes.Roll    | 0x00000010  | Roll        |
| GameInputFlightStickAxes.Pitch    | 0x00000020  | Pitch       |
| GameInputFlightStickAxes.Yaw      | 0x00000040  | Yaw         |
| GameInputFlightStickAxes.Throttle | 0x00000080  | Throttle    |

**Buttons** — `InputMapper.GetFlightStickButtonMappingInfo(buttonElement)`:

| Constant                                    | Value        | Description           |
| ------------------------------------------- | ------------ | --------------------- |
| GameInputFlightStickButtons.None            | 0            | No button             |
| GameInputFlightStickButtons.Menu            | 0x00000001   | Menu                  |
| GameInputFlightStickButtons.View            | 0x00000002   | View                  |
| GameInputFlightStickButtons.FirePrimary     | 0x00000004   | Primary fire          |
| GameInputFlightStickButtons.FireSecondary  | 0x00000008   | Secondary fire        |
| GameInputFlightStickButtons.HatSwitchUp/Down/Left/Right | 0x00000010 … | Hat switch            |
| GameInputFlightStickButtons.A, B, X, Y      | 0x00000100 … | Gamepad-style (if mapped) |

---

## Racing wheel

**Axes** — `InputMapper.GetRacingWheelAxisMappingInfo(axisElement)`:

| Constant                              | Value        | Description     |
| ------------------------------------- | ------------ | --------------- |
| GameInputRacingWheelAxes.None         | 0            | No axis         |
| GameInputRacingWheelAxes.Steering     | 0x00000100   | Steering wheel  |
| GameInputRacingWheelAxes.Throttle    | 0x00000200   | Throttle        |
| GameInputRacingWheelAxes.Brake        | 0x00000400   | Brake           |
| GameInputRacingWheelAxes.Clutch       | 0x00000800   | Clutch          |
| GameInputRacingWheelAxes.Handbrake    | 0x00001000   | Handbrake       |
| GameInputRacingWheelAxes.PatternShifter | 0x00002000   | Pattern shifter |

**Buttons** — `InputMapper.GetRacingWheelButtonMappingInfo(buttonElement)`:

| Constant                                               | Value        | Description           |
| ------------------------------------------------------ | ------------ | --------------------- |
| GameInputRacingWheelButtons.None                      | 0            | No button             |
| GameInputRacingWheelButtons.Menu                       | 0x00000001   | Menu                  |
| GameInputRacingWheelButtons.View                       | 0x00000002   | View                  |
| GameInputRacingWheelButtons.PreviousGear               | 0x00000004   | Previous gear         |
| GameInputRacingWheelButtons.NextGear                  | 0x00000008   | Next gear             |
| GameInputRacingWheelButtons.DpadUp/Down/Left/Right     | 0x00000010 … | D-pad                 |
| GameInputRacingWheelButtons.A, B, X, Y, shoulders, thumbsticks | 0x00000100 … | Gamepad-style (if mapped) |

---

## Arcade stick

Arcade stick has **no axes** in the mapping API (stick directions are exposed as buttons).

**Buttons** — `InputMapper.GetArcadeStickButtonMappingInfo(buttonElement)`:

| Constant                                   | Value              | Description           |
| ------------------------------------------ | ------------------ | --------------------- |
| GameInputArcadeStickButtons.None           | 0                  | Neutral               |
| GameInputArcadeStickButtons.Menu           | 0x00000001         | Menu                  |
| GameInputArcadeStickButtons.View           | 0x00000002         | View                  |
| GameInputArcadeStickButtons.Up             | 0x00000004         | Stick up              |
| GameInputArcadeStickButtons.Down           | 0x00000008         | Stick down            |
| GameInputArcadeStickButtons.Left           | 0x00000010         | Stick left            |
| GameInputArcadeStickButtons.Right          | 0x00000020         | Stick right           |
| GameInputArcadeStickButtons.Action1 … Action6 | 0x00000040 …   | Action buttons 1–6    |
| GameInputArcadeStickButtons.Special1, Special2 | 0x00001000, 0x00002000 | Special buttons |

---

## Usage in code

All of the above are defined in **`GameInputSharp.Abstractions`** as enums (e.g. `GameInputGamepadAxes`, `GameInputGamepadButtons`). Cast to `int` when calling the mapper:

```csharp
using (var mapper = gamepad.CreateInputMapper())
{
    if (mapper != null)
    {
        // Which physical controller element drives left thumbstick X?
        var leftStickX = mapper.GetGamepadAxisMappingInfo((int)GameInputGamepadAxes.LeftThumbstickX);
        if (leftStickX.HasValue)
        {
            // leftStickX.Value.ControllerElementKind == GameInputElementKind.Axis etc.
        }

        // Which physical element drives the A button?
        var buttonA = mapper.GetGamepadButtonMappingInfo((int)GameInputGamepadButtons.A);
    }
}
```

For the **gamepad state button bitmask** (e.g. `GamepadState.Buttons`), use the same `GameInputGamepadButtons` values when testing bits; see [USAGE.md](USAGE.md).
