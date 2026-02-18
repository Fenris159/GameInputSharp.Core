using GameInputSharp.Devices;

namespace GameInputSharp.Haptics;

/// <summary>
/// Basic haptics: simple left/right rumble.
/// </summary>
/// <remarks>
/// Obtain from <see cref="Devices.GamepadDevice.Haptics"/>. Values in [0,1]; 0 stops the motor.
/// </remarks>
public sealed class BasicHaptics
{
    private readonly GamepadDevice _gamepad;

    internal BasicHaptics(GamepadDevice gamepad)
    {
        _gamepad = gamepad ?? throw new ArgumentNullException(nameof(gamepad));
    }

    /// <summary>Sets simple rumble. Left and right in [0,1].</summary>
    /// <param name="left">Left (low-frequency) motor strength, 0 to 1.</param>
    /// <param name="right">Right (high-frequency) motor strength, 0 to 1.</param>
    public void SetVibration(float left, float right)
    {
        _gamepad.SetVibration(left, right);
    }
}
