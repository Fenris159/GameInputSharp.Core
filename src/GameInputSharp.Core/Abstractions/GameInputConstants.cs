// Public constants for GameInput API (input kinds, focus policy). Match Microsoft GameInput v3.

namespace GameInputSharp.Abstractions;

/// <summary>Input kind flags for GetCurrentReading, CreateAggregateDevice, etc. Match GameInputKind in the native API.</summary>
public static class GameInputKinds
{
    /// <summary>Unknown.</summary>
    public const uint Unknown = 0x00000000;
    /// <summary>Raw device reports.</summary>
    public const uint RawDeviceReport = 0x00000001;
    /// <summary>Generic controller axes.</summary>
    public const uint ControllerAxis = 0x00000002;
    /// <summary>Generic controller buttons.</summary>
    public const uint ControllerButton = 0x00000004;
    /// <summary>Generic controller switches.</summary>
    public const uint ControllerSwitch = 0x00000008;
    /// <summary>Generic controller (axis, button, switch).</summary>
    public const uint Controller = 0x0000000E;
    /// <summary>Keyboard.</summary>
    public const uint Keyboard = 0x00000010;
    /// <summary>Mouse.</summary>
    public const uint Mouse = 0x00000020;
    /// <summary>Sensors (motion, gyro, orientation).</summary>
    public const uint Sensors = 0x00000040;
    /// <summary>Arcade stick.</summary>
    public const uint ArcadeStick = 0x00010000;
    /// <summary>Flight stick.</summary>
    public const uint FlightStick = 0x00020000;
    /// <summary>Gamepad.</summary>
    public const uint Gamepad = 0x00040000;
    /// <summary>Racing wheel.</summary>
    public const uint RacingWheel = 0x00080000;
    /// <summary>UI navigation.</summary>
    public const uint UiNavigation = 0x01000000;
}

/// <summary>Focus policy for SetFocusPolicy. Match GameInputFocusPolicy in the native API.</summary>
public static class GameInputFocusPolicy
{
    /// <summary>Default.</summary>
    public const uint Default = 0;
    /// <summary>Background.</summary>
    public const uint Background = 1;
    /// <summary>Exclusive.</summary>
    public const uint Exclusive = 2;
}

/// <summary>Device status flags returned by GetDeviceStatus() and in DeviceCallbackEventArgs. Match GameInputDeviceStatus in the native API.</summary>
public static class GameInputDeviceStatus
{
    /// <summary>No status / disconnected.</summary>
    public const uint NoStatus = 0x00000000;
    /// <summary>Device is connected.</summary>
    public const uint Connected = 0x00000001;
    /// <summary>Match any status (e.g. for callback registration filter).</summary>
    public const uint AnyStatus = 0xFFFFFFFF;
}
