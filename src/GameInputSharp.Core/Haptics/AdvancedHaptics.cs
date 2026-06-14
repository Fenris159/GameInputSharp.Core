using GameInputSharp.Core;
using GameInputSharp.Devices;

namespace GameInputSharp.Haptics;

/// <summary>
/// Advanced haptics: waveforms, multi-motor control, audio-driven playback.
/// </summary>
/// <remarks>
/// Maps to IGameInputDevice.CreateForceFeedbackEffect. Up to 8 locations per device.
/// </remarks>
public sealed class AdvancedHaptics
{
    private readonly GamepadDevice _gamepad;

    /// <summary>Creates advanced haptics for the given gamepad.</summary>
    /// <param name="gamepad">The gamepad device (must support force feedback).</param>
    public AdvancedHaptics(GamepadDevice gamepad)
    {
        _gamepad = gamepad ?? throw new ArgumentNullException(nameof(gamepad));
    }

    /// <summary>Plays a waveform on the given locations (max 8 per device).</summary>
    /// <param name="waveformData">Optional. If length >= 8: first 4 bytes = duration ms (little-endian), next 4 = intensity [0,1] (float). Otherwise defaults: 100 ms, 0.5 intensity.</param>
    /// <param name="locations">Target haptic locations (up to 8).</param>
    public void PlayHapticWaveform(byte[]? waveformData, HapticLocation[] locations)
    {
        if (locations == null || locations.Length == 0)
            return;

        uint durationMs = 100;
        float intensity = 0.5f;
        if (waveformData != null && waveformData.Length >= 8)
        {
            durationMs = BitConverter.ToUInt32(waveformData, 0);
            if (durationMs == 0) durationMs = 100;
            intensity = BitConverter.ToSingle(waveformData, 4);
            intensity = Math.Clamp(intensity, 0f, 1f);
        }

        ulong durationUs = durationMs * 1000UL;
        IntPtr devicePtr = _gamepad.DevicePtr;
        if (devicePtr == IntPtr.Zero)
            return;

        foreach (var loc in locations)
        {
            int motorIndex = (int)loc;
            if (motorIndex < 0 || motorIndex > 7)
                continue;
            GameInputInterop.PlayForceFeedbackConstant(devicePtr, motorIndex, durationUs, intensity);
        }
    }
}
