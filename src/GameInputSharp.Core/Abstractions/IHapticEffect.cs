namespace GameInputSharp.Abstractions;

/// <summary>
/// Represents a haptic effect that can be played on supported devices.
/// Covers simple rumble and advanced waveforms/multi-motor.
/// </summary>
/// <remarks>
/// Basic rumble is exposed via <see cref="Devices.GamepadDevice.Haptics"/> and <see cref="Haptics.BasicHaptics.SetVibration"/>.
/// Advanced waveform effects use <see cref="Haptics.AdvancedHaptics"/>.
/// </remarks>
public interface IHapticEffect : IDisposable
{
    /// <summary>Whether this effect is currently playing.</summary>
    bool IsPlaying { get; }

    /// <summary>Stops the effect if playing.</summary>
    void Stop();
}
