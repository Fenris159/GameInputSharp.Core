// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.
// Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

namespace GameInputSharp.Abstractions;

/// <summary>Current gamepad state (buttons, triggers, thumbsticks).</summary>
/// <remarks>Obtain via <see cref="GameInputManager.GetCurrentGamepadState"/> or <see cref="GameInputManager.GetGamepadStateFromReading"/>. Use <see cref="GameInputGamepadButtons"/> (e.g. A, B, DPadUp) to test <see cref="Buttons"/> bits.</remarks>
public struct GamepadState
{
    /// <summary>Button flags. Test with <see cref="GameInputGamepadButtons"/> (e.g. <c>(Buttons &amp; (uint)GameInputGamepadButtons.A) != 0</c>).</summary>
    public uint Buttons;

    /// <summary>Left trigger [0,1].</summary>
    public float LeftTrigger;

    /// <summary>Right trigger [0,1].</summary>
    public float RightTrigger;

    /// <summary>Left stick X [-1,1].</summary>
    public float LeftThumbstickX;

    /// <summary>Left stick Y [-1,1].</summary>
    public float LeftThumbstickY;

    /// <summary>Right stick X [-1,1].</summary>
    public float RightThumbstickX;

    /// <summary>Right stick Y [-1,1].</summary>
    public float RightThumbstickY;
}

/// <summary>Current mouse state (buttons, position, wheel).</summary>
/// <remarks>Obtain via <see cref="GameInputManager.GetCurrentMouseState"/>.</remarks>
public struct MouseState
{
    /// <summary>Button flags.</summary>
    public uint Buttons;

    /// <summary>Relative position X.</summary>
    public long PositionX;

    /// <summary>Relative position Y.</summary>
    public long PositionY;

    /// <summary>Absolute position X.</summary>
    public long AbsolutePositionX;

    /// <summary>Absolute position Y.</summary>
    public long AbsolutePositionY;

    /// <summary>Wheel delta X.</summary>
    public long WheelX;

    /// <summary>Wheel delta Y.</summary>
    public long WheelY;
}

/// <summary>Single key state from a keyboard reading.</summary>
public struct KeyState
{
    /// <summary>Scan code.</summary>
    public uint ScanCode;

    /// <summary>Unicode code point.</summary>
    public uint CodePoint;

    /// <summary>Virtual key code.</summary>
    public byte VirtualKey;

    /// <summary>Whether this is a dead key.</summary>
    public bool IsDeadKey;
}

/// <summary>Sensors (motion) state: acceleration, angular velocity, heading, orientation. Obtain via <see cref="GameInputManager.GetCurrentSensorsState"/>.</summary>
public struct SensorsState
{
    /// <summary>Acceleration in G (X, Y, Z).</summary>
    public float AccelerationInGX, AccelerationInGY, AccelerationInGZ;
    /// <summary>Angular velocity in rad/s (X, Y, Z).</summary>
    public float AngularVelocityInRadPerSecX, AngularVelocityInRadPerSecY, AngularVelocityInRadPerSecZ;
    /// <summary>Heading in degrees from magnetic north.</summary>
    public float HeadingInDegreesFromMagneticNorth;
    /// <summary>Heading accuracy.</summary>
    public uint HeadingAccuracy;
    /// <summary>Orientation quaternion (W, X, Y, Z).</summary>
    public float OrientationW, OrientationX, OrientationY, OrientationZ;
}

/// <summary>Arcade stick state (buttons). Obtain via <see cref="GameInputManager.GetCurrentArcadeStickState"/>.</summary>
public struct ArcadeStickState
{
    /// <summary>Button flags.</summary>
    public uint Buttons;
}

/// <summary>Flight stick state. Obtain via <see cref="GameInputManager.GetCurrentFlightStickState"/>.</summary>
public struct FlightStickState
{
    /// <summary>Button flags.</summary>
    public uint Buttons;
    /// <summary>Hat switch.</summary>
    public int HatSwitch;
    /// <summary>Roll, pitch, yaw, throttle.</summary>
    public float Roll, Pitch, Yaw, Throttle;
}

/// <summary>Racing wheel state. Obtain via <see cref="GameInputManager.GetCurrentRacingWheelState"/>.</summary>
public struct RacingWheelState
{
    /// <summary>Button flags.</summary>
    public uint Buttons;
    /// <summary>Pattern shifter gear.</summary>
    public int PatternShifterGear;
    /// <summary>Wheel, throttle, brake, clutch, handbrake.</summary>
    public float Wheel, Throttle, Brake, Clutch, Handbrake;
}

/// <summary>Haptic device info: location count and optional audio endpoint. Obtain via <see cref="Devices.GamepadDevice.GetHapticInfo"/>.</summary>
public struct HapticInfo
{
    /// <summary>Number of haptic locations (motors), typically up to 8.</summary>
    public uint LocationCount;
    /// <summary>Location GUIDs (up to 8).</summary>
    public Guid[]? LocationIds;
    /// <summary>Optional audio endpoint ID for audio-driven haptics.</summary>
    public string? AudioEndpointId;
}
