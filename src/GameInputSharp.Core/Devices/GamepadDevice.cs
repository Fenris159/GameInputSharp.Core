using GameInputSharp.Abstractions;
using GameInputSharp.Core;
using GameInputSharp.Haptics;

namespace GameInputSharp.Devices;

/// <summary>
/// Wrapper for a gamepad/controller: buttons, axes, triggers, simple rumble via <see cref="Haptics"/>.
/// </summary>
/// <remarks>
/// Use <see cref="Haptics"/> and <see cref="GameInputSharp.Haptics.BasicHaptics.SetVibration(System.Single,System.Single)"/> for left/right rumble. Dispose when done to release the native device.
/// </remarks>
public sealed class GamepadDevice : IInputDevice
{
    private IntPtr _devicePtr;
    private bool _disposed;

    internal GamepadDevice(IntPtr devicePtr, string deviceId, string displayName)
    {
        _devicePtr = devicePtr;
        DeviceId = deviceId;
        DisplayName = displayName;
    }

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public bool IsConnected => _devicePtr != IntPtr.Zero && !_disposed;

    /// <summary>Gets basic haptics for this device (simple rumble).</summary>
    public BasicHaptics Haptics => new BasicHaptics(this);

    /// <summary>Gets advanced haptics for this device (waveforms, multi-motor).</summary>
    public AdvancedHaptics AdvancedHaptics => new AdvancedHaptics(this);

    /// <summary>Sets simple rumble. Left/right in [0,1].</summary>
    internal void SetVibration(float left, float right)
    {
        if (_devicePtr == IntPtr.Zero || _disposed) return;
        GameInputInterop.SetRumble(_devicePtr, left, right, 0f, 0f);
    }

    /// <summary>Gets the native device pointer for advanced use. Do not release.</summary>
    internal IntPtr DevicePtr => _devicePtr;

    /// <inheritdoc />
    public IntPtr GetDevicePointer() => _devicePtr;

    /// <inheritdoc />
    public DeviceInfo? GetDeviceInfo()
    {
        if (_devicePtr == IntPtr.Zero || _disposed) return null;
        return GameInputInterop.GetFullDeviceInfo(_devicePtr);
    }

    /// <summary>Gets the current device status flags (e.g. GameInputDeviceConnected).</summary>
    public uint GetDeviceStatus()
    {
        if (_devicePtr == IntPtr.Zero || _disposed) return 0;
        return GameInputInterop.GetDeviceStatus(_devicePtr);
    }

    /// <summary>Gets haptic info (location count, location GUIDs, optional audio endpoint).</summary>
    public Abstractions.HapticInfo GetHapticInfo()
    {
        var result = new Abstractions.HapticInfo { LocationCount = 0 };
        if (_devicePtr == IntPtr.Zero || _disposed) return result;
        if (!GameInputInterop.GetHapticInfo(_devicePtr, out var native))
            return result;
        result.LocationCount = native.LocationCount;
        if (native.Locations != null && native.LocationCount > 0)
        {
            int n = (int)Math.Min(native.LocationCount, (uint)native.Locations.Length);
            result.LocationIds = new Guid[n];
            for (int i = 0; i < n; i++)
                result.LocationIds[i] = native.Locations[i];
        }
        if (native.AudioEndpointId != null && native.AudioEndpointId.Length > 0)
        {
            int len = Array.IndexOf(native.AudioEndpointId, '\0');
            if (len < 0) len = native.AudioEndpointId.Length;
            if (len > 0)
                result.AudioEndpointId = new string(native.AudioEndpointId, 0, len);
        }
        return result;
    }

    /// <summary>Plays a constant force feedback effect on a motor (fire-and-forget). For controllable effects use <see cref="CreateForceFeedbackEffect(uint,ulong,float)"/>.</summary>
    /// <param name="motorIndex">Motor index 0–7.</param>
    /// <param name="durationMicroseconds">Sustain duration in microseconds.</param>
    /// <param name="intensity">Magnitude 0–1.</param>
    /// <returns>True if the effect was started.</returns>
    public bool PlayForceFeedbackConstant(int motorIndex, ulong durationMicroseconds, float intensity)
    {
        if (_devicePtr == IntPtr.Zero || _disposed || motorIndex < 0 || motorIndex > 7) return false;
        return GameInputInterop.PlayForceFeedbackConstant(_devicePtr, motorIndex, durationMicroseconds, Math.Clamp(intensity, 0f, 1f));
    }

    /// <summary>Sets the master gain for a force feedback motor (0–1).</summary>
    /// <param name="motorIndex">Motor index 0–7.</param>
    /// <param name="gain">Master gain 0–1.</param>
    public void SetForceFeedbackMotorGain(uint motorIndex, float gain)
    {
        if (_devicePtr == IntPtr.Zero || _disposed) return;
        GameInputInterop.SetForceFeedbackMotorGain(_devicePtr, motorIndex, gain);
    }

    /// <summary>Returns whether the force feedback motor is powered on.</summary>
    public bool IsForceFeedbackMotorPoweredOn(uint motorIndex)
    {
        if (_devicePtr == IntPtr.Zero || _disposed) return false;
        return GameInputInterop.IsForceFeedbackMotorPoweredOn(_devicePtr, motorIndex);
    }

    /// <summary>Creates a constant force feedback effect you can control (start, pause, resume, stop, set gain). Dispose the returned effect when done. For fire-and-forget use <see cref="AdvancedHaptics.PlayHapticWaveform"/>.</summary>
    /// <param name="motorIndex">Motor index 0–7.</param>
    /// <param name="durationMicroseconds">Sustain duration in microseconds.</param>
    /// <param name="intensity">Magnitude 0–1.</param>
    /// <returns>A controllable effect, or null if creation failed. Call <see cref="ForceFeedbackEffect.Start"/> to play; use <see cref="ForceFeedbackEffect.Pause"/>/<see cref="ForceFeedbackEffect.Start"/> to pause/resume, <see cref="ForceFeedbackEffect.Stop"/> to stop, <see cref="ForceFeedbackEffect.SetGain"/> to change intensity.</returns>
    public ForceFeedbackEffect? CreateForceFeedbackEffect(uint motorIndex, ulong durationMicroseconds, float intensity)
    {
        if (_devicePtr == IntPtr.Zero || _disposed || motorIndex > 7) return null;
        IntPtr effectPtr = GameInputInterop.CreateForceFeedbackEffectConstantRetain(_devicePtr, (int)motorIndex, durationMicroseconds, Math.Clamp(intensity, 0f, 1f));
        if (effectPtr == IntPtr.Zero) return null;
        return new ForceFeedbackEffect(effectPtr, GameInputInterop.ReleaseForceFeedbackEffect);
    }

    /// <summary>Creates a force feedback effect from full constant params (envelope + magnitude). Returns a controllable effect; dispose when done.</summary>
    public ForceFeedbackEffect? CreateForceFeedbackEffect(uint motorIndex, in ForceFeedbackConstantParams parameters)
    {
        if (_devicePtr == IntPtr.Zero || _disposed || motorIndex > 7) return null;
        IntPtr effectPtr = GameInputInterop.CreateForceFeedbackEffectFromConstant(_devicePtr, (int)motorIndex, parameters);
        if (effectPtr == IntPtr.Zero) return null;
        return new ForceFeedbackEffect(effectPtr, GameInputInterop.ReleaseForceFeedbackEffect);
    }

    /// <summary>Creates a ramp force feedback effect (force goes from start to end magnitude). Returns a controllable effect; dispose when done.</summary>
    public ForceFeedbackEffect? CreateForceFeedbackEffect(uint motorIndex, in ForceFeedbackRampParams parameters)
    {
        if (_devicePtr == IntPtr.Zero || _disposed || motorIndex > 7) return null;
        IntPtr effectPtr = GameInputInterop.CreateForceFeedbackEffectFromRamp(_devicePtr, (int)motorIndex, parameters);
        if (effectPtr == IntPtr.Zero) return null;
        return new ForceFeedbackEffect(effectPtr, GameInputInterop.ReleaseForceFeedbackEffect);
    }

    /// <summary>Creates a periodic force feedback effect (sine, square, triangle, sawtooth). Use <paramref name="kind"/> = SineWave, SquareWave, TriangleWave, SawtoothUpWave, or SawtoothDownWave. Returns a controllable effect; dispose when done.</summary>
    public ForceFeedbackEffect? CreateForceFeedbackEffect(uint motorIndex, ForceFeedbackEffectKind kind, in ForceFeedbackPeriodicParams parameters)
    {
        if (_devicePtr == IntPtr.Zero || _disposed || motorIndex > 7) return null;
        IntPtr effectPtr = GameInputInterop.CreateForceFeedbackEffectFromPeriodic(_devicePtr, (int)motorIndex, kind, parameters);
        if (effectPtr == IntPtr.Zero) return null;
        return new ForceFeedbackEffect(effectPtr, GameInputInterop.ReleaseForceFeedbackEffect);
    }

    /// <summary>Creates a condition force feedback effect (spring, friction, damper, inertia). Use <paramref name="kind"/> = Spring, Friction, Damper, or Inertia. Common for racing wheels and flight sticks. Returns a controllable effect; dispose when done.</summary>
    public ForceFeedbackEffect? CreateForceFeedbackEffect(uint motorIndex, ForceFeedbackEffectKind kind, in ForceFeedbackConditionParams parameters)
    {
        if (_devicePtr == IntPtr.Zero || _disposed || motorIndex > 7) return null;
        IntPtr effectPtr = GameInputInterop.CreateForceFeedbackEffectFromCondition(_devicePtr, (int)motorIndex, kind, parameters);
        if (effectPtr == IntPtr.Zero) return null;
        return new ForceFeedbackEffect(effectPtr, GameInputInterop.ReleaseForceFeedbackEffect);
    }

    /// <summary>Gets the number of extra axes for an input kind. Use <see cref="Abstractions.GameInputKinds"/> for inputKind.</summary>
    public uint GetExtraAxisCount(uint inputKind) => GameInputInterop.GetExtraAxisCount(_devicePtr, inputKind);

    /// <summary>Gets the number of extra buttons for an input kind.</summary>
    public uint GetExtraButtonCount(uint inputKind) => GameInputInterop.GetExtraButtonCount(_devicePtr, inputKind);

    /// <summary>Gets the extra axis indexes for an input kind.</summary>
    public uint[] GetExtraAxisIndexes(uint inputKind) => GameInputInterop.GetExtraAxisIndexes(_devicePtr, inputKind);

    /// <summary>Gets the extra button indexes for an input kind.</summary>
    public uint[] GetExtraButtonIndexes(uint inputKind) => GameInputInterop.GetExtraButtonIndexes(_devicePtr, inputKind);

    /// <summary>DirectInput escape: send a command to the device. Returns (success, bytesWritten).</summary>
    public (bool Success, uint BytesWritten) DirectInputEscape(uint command, byte[]? bufferIn, byte[]? bufferOut) =>
        GameInputInterop.DirectInputEscape(_devicePtr, command, bufferIn, bufferOut);

    /// <summary>Creates an input mapper to query axis/button mapping. Dispose the returned instance when done.</summary>
    public InputMapper? CreateInputMapper()
    {
        if (_devicePtr == IntPtr.Zero || _disposed) return null;
        IntPtr ptr = GameInputInterop.CreateInputMapper(_devicePtr);
        return ptr != IntPtr.Zero ? new InputMapper(ptr) : null;
    }

    /// <summary>Creates a raw device report. Dispose the returned report when done.</summary>
    public RawDeviceReport? CreateRawDeviceReport(uint reportId, int reportKind)
    {
        if (_devicePtr == IntPtr.Zero || _disposed) return null;
        IntPtr ptr = GameInputInterop.CreateRawDeviceReport(_devicePtr, reportId, reportKind);
        return ptr != IntPtr.Zero ? new RawDeviceReport(ptr) : null;
    }

    /// <summary>Sends a raw device output report. The report is not disposed by this call.</summary>
    public bool SendRawDeviceOutput(RawDeviceReport? report) =>
        report != null && GameInputInterop.SendRawDeviceOutput(_devicePtr, report.UnsafePointer);

    /// <summary>Sends a raw device output report by pointer. The report pointer is not released by this call.</summary>
    public bool SendRawDeviceOutput(IntPtr reportPtr) => GameInputInterop.SendRawDeviceOutput(_devicePtr, reportPtr);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        if (_devicePtr != IntPtr.Zero)
        {
            GameInputInterop.ReleaseDevice(_devicePtr);
            _devicePtr = IntPtr.Zero;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~GamepadDevice()
    {
        if (_devicePtr != IntPtr.Zero)
        {
            GameInputInterop.ReleaseDevice(_devicePtr);
            _devicePtr = IntPtr.Zero;
        }
        _disposed = true;
    }
}
