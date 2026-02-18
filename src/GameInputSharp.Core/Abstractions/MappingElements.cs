// Mapping element constants for IGameInputMapper (axes, buttons, switches).
// Values match Microsoft GameInput v3 (GameInput.h). Use with InputMapper.Get*MappingInfo.

namespace GameInputSharp.Abstractions;

/// <summary>Kind of controller element (axis, button, switch). Used in <see cref="AxisMappingInfo"/> and <see cref="ButtonMappingInfo"/>.</summary>
public enum GameInputElementKind
{
    None = 0,
    Axis = 1,
    Button = 2,
    Switch = 3
}

/// <summary>Switch position (e.g. hat switch). Used in mapping info and controller switch state.</summary>
public enum GameInputSwitchPosition
{
    Center = 0,
    Up = 1,
    UpRight = 2,
    Right = 3,
    DownRight = 4,
    Down = 5,
    DownLeft = 6,
    Left = 7,
    UpLeft = 8
}

/// <summary>Gamepad axes. Pass to <see cref="InputMapper.GetGamepadAxisMappingInfo"/>.</summary>
[Flags]
public enum GameInputGamepadAxes
{
    None = 0x00000000,
    LeftTrigger = 0x00000001,
    RightTrigger = 0x00000002,
    LeftThumbstickX = 0x00000004,
    LeftThumbstickY = 0x00000008,
    RightThumbstickX = 0x00000010,
    RightThumbstickY = 0x00000020
}

/// <summary>Gamepad buttons (bitmask). Pass to <see cref="InputMapper.GetGamepadButtonMappingInfo"/>.</summary>
[Flags]
public enum GameInputGamepadButtons
{
    None = 0x00000000,
    Menu = 0x00000001,
    View = 0x00000002,
    A = 0x00000004,
    B = 0x00000008,
    X = 0x00000010,
    Y = 0x00000020,
    DPadUp = 0x00000040,
    DPadDown = 0x00000080,
    DPadLeft = 0x00000100,
    DPadRight = 0x00000200,
    LeftShoulder = 0x00000400,
    RightShoulder = 0x00000800,
    LeftThumbstick = 0x00001000,
    RightThumbstick = 0x00002000,
    C = 0x00004000,
    Z = 0x00008000,
    LeftTriggerButton = 0x00010000,
    RightTriggerButton = 0x00020000,
    LeftThumbstickUp = 0x00040000,
    LeftThumbstickDown = 0x00080000,
    LeftThumbstickLeft = 0x00100000,
    LeftThumbstickRight = 0x00200000,
    RightThumbstickUp = 0x00400000,
    RightThumbstickDown = 0x00800000,
    RightThumbstickLeft = 0x01000000,
    RightThumbstickRight = 0x02000000,
    PaddleLeft1 = 0x04000000,
    PaddleLeft2 = 0x08000000,
    PaddleRight1 = 0x10000000,
    PaddleRight2 = 0x20000000
}

/// <summary>Flight stick axes. Pass to <see cref="InputMapper.GetFlightStickAxisMappingInfo"/>.</summary>
[Flags]
public enum GameInputFlightStickAxes
{
    None = 0x00000000,
    Roll = 0x00000010,
    Pitch = 0x00000020,
    Yaw = 0x00000040,
    Throttle = 0x00000080
}

/// <summary>Flight stick buttons. Pass to <see cref="InputMapper.GetFlightStickButtonMappingInfo"/>.</summary>
[Flags]
public enum GameInputFlightStickButtons
{
    None = 0x00000000,
    Menu = 0x00000001,
    View = 0x00000002,
    FirePrimary = 0x00000004,
    FireSecondary = 0x00000008,
    HatSwitchUp = 0x00000010,
    HatSwitchDown = 0x00000020,
    HatSwitchLeft = 0x00000040,
    HatSwitchRight = 0x00000080,
    A = 0x00000100,
    B = 0x00000200,
    X = 0x00000400,
    Y = 0x00000800
}

/// <summary>Racing wheel axes. Pass to <see cref="InputMapper.GetRacingWheelAxisMappingInfo"/>.</summary>
[Flags]
public enum GameInputRacingWheelAxes
{
    None = 0x00000000,
    Steering = 0x00000100,
    Throttle = 0x00000200,
    Brake = 0x00000400,
    Clutch = 0x00000800,
    Handbrake = 0x00001000,
    PatternShifter = 0x00002000
}

/// <summary>Racing wheel buttons. Pass to <see cref="InputMapper.GetRacingWheelButtonMappingInfo"/>.</summary>
[Flags]
public enum GameInputRacingWheelButtons
{
    None = 0x00000000,
    Menu = 0x00000001,
    View = 0x00000002,
    PreviousGear = 0x00000004,
    NextGear = 0x00000008,
    DpadUp = 0x00000010,
    DpadDown = 0x00000020,
    DpadLeft = 0x00000040,
    DpadRight = 0x00000080,
    A = 0x00000100,
    B = 0x00000200,
    X = 0x00000400,
    Y = 0x00000800,
    LeftShoulder = 0x00001000,
    RightShoulder = 0x00002000,
    LeftThumbstick = 0x00004000,
    RightThumbstick = 0x00008000
}

/// <summary>Arcade stick buttons (arcade stick has no axes). Pass to <see cref="InputMapper.GetArcadeStickButtonMappingInfo"/>.</summary>
[Flags]
public enum GameInputArcadeStickButtons
{
    None = 0x00000000,
    Menu = 0x00000001,
    View = 0x00000002,
    Up = 0x00000004,
    Down = 0x00000008,
    Left = 0x00000010,
    Right = 0x00000020,
    Action1 = 0x00000040,
    Action2 = 0x00000080,
    Action3 = 0x00000100,
    Action4 = 0x00000200,
    Action5 = 0x00000400,
    Action6 = 0x00000800,
    Special1 = 0x00001000,
    Special2 = 0x00002000
}
