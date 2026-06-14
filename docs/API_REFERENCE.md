# API constants and flags reference

Quick reference for **input kinds**, **focus policy**, **device status**, and pointers to other references. All constants are in `GameInputSharp.Abstractions` unless noted.

---

## Input kinds (`GameInputKinds`)

Used for: `GetCurrentReading` / state APIs, `RegisterReadingCallback`, `CreateAggregateDevice`, `GetExtraAxisCount` / `GetExtraButtonCount` (and indexes), device filtering. Values are flags; combine with bitwise OR if needed.

| Constant | Value | Use |
|----------|-------|-----|
| Unknown | 0x00000000 | No specific kind |
| Controller | 0x0000000E | Generic controller (axis/button/switch) |
| Keyboard | 0x00000010 | Keyboard |
| Mouse | 0x00000020 | Mouse |
| Sensors | 0x00000040 | Motion, gyro, orientation |
| ArcadeStick | 0x00010000 | Arcade stick |
| FlightStick | 0x00020000 | Flight stick |
| Gamepad | 0x00040000 | Gamepad |
| RacingWheel | 0x00080000 | Racing wheel |
| UiNavigation | 0x01000000 | UI navigation |

---

## Focus policy (`GameInputFocusPolicy`)

Pass to `GameInputManager.SetFocusPolicy(uint)` to control whether the app receives input when in background.

| Constant | Value | Description |
|----------|-------|--------------|
| Default | 0 | Normal focus behavior |
| Background | 1 | Receive input in background |
| Exclusive | 2 | Exclusive focus |

---

## Device status (`GameInputDeviceStatus`)

Returned by `GetDeviceStatus()` and in `DeviceCallbackEventArgs.CurrentStatus` / `PreviousStatus`. Use when filtering device callbacks or checking connection.

| Constant | Value | Description |
|----------|-------|--------------|
| NoStatus | 0x00000000 | No status / disconnected |
| Connected | 0x00000001 | Device is connected |
| AnyStatus | 0xFFFFFFFF | Match any status (e.g. callback registration filter) |

Example:

```csharp
uint status = gamepad.GetDeviceStatus();
bool connected = (status & GameInputDeviceStatus.Connected) != 0;

// In device callback
if ((args.CurrentStatus & GameInputDeviceStatus.Connected) != 0)
    Console.WriteLine("Device connected");
```

---

## Force feedback

- **Effect kinds:** `GameInputSharp.Haptics.ForceFeedbackEffectKind` (Constant, Ramp, SineWave, SquareWave, TriangleWave, SawtoothUpWave, SawtoothDownWave, Spring, Friction, Damper, Inertia).
- **Params and envelope:** `ForceFeedbackEnvelope`, `ForceFeedbackConstantParams`, etc. in `GameInputSharp.Haptics`. Envelope durations are in **100-nanosecond units** (1 µs = 10 units).
- **Usage:** See [USAGE.md](USAGE.md) — “All force feedback effect kinds (full complexity)”.

---

## Axis and button mapping

- **Device control layouts** (gamepad, flight stick, racing wheel, arcade stick): [MAPPING_REFERENCE.md](MAPPING_REFERENCE.md).
- **Enums:** `GameInputGamepadAxes`, `GameInputGamepadButtons`, `GameInputFlightStickAxes`, `GameInputFlightStickButtons`, `GameInputRacingWheelAxes`, `GameInputRacingWheelButtons`, `GameInputArcadeStickButtons`, `GameInputElementKind`, `GameInputSwitchPosition` in `GameInputSharp.Abstractions`.

---

## Other docs

| Topic | Document |
|-------|----------|
| Setup, polling, haptics, callbacks | [USAGE.md](USAGE.md) |
| Device control layouts (axes/buttons) | [MAPPING_REFERENCE.md](MAPPING_REFERENCE.md) |
| Audit and gaps | [AUDIT.md](AUDIT.md) |
| API alignment with Microsoft docs | [API_ALIGNMENT.md](API_ALIGNMENT.md) |
| Changelog (Core only) | [CHANGELOG.md](CHANGELOG.md) |
| Security and safety | [SECURITY.md](SECURITY.md) |
| Unity, MonoGame, WPF, etc. | [COMPATIBILITY.md](COMPATIBILITY.md) |
