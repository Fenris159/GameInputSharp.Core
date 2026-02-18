// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.

using GameInputSharp.Core;

namespace GameInputSharp.Haptics;

/// <summary>State of a force feedback effect (matches GameInputFeedbackEffectState).</summary>
public enum ForceFeedbackEffectState
{
    /// <summary>Effect is stopped.</summary>
    Stopped = 0,
    /// <summary>Effect is playing.</summary>
    Running = 1,
    /// <summary>Effect is paused (can be resumed).</summary>
    Paused = 2
}

/// <summary>
/// Wraps a native force feedback effect. Gives C# the same granular control as C++ IGameInputForceFeedbackEffect:
/// pause/resume/stop, per-effect gain, and state query. Dispose when done to release the native effect.
/// </summary>
/// <remarks>
/// Create via <see cref="Devices.GamepadDevice.CreateForceFeedbackEffect(uint, ulong, float)"/> or other overloads for constant, ramp, periodic, or condition effects. You can start the effect with
/// <see cref="Start"/>, then <see cref="Pause"/> / <see cref="Start"/> to pause/resume, <see cref="Stop"/> to stop,
/// and <see cref="SetGain"/> to change intensity. <see cref="State"/> reports whether the effect is running, paused, or stopped.
/// </remarks>
public sealed class ForceFeedbackEffect : IDisposable
{
    private IntPtr _effectPtr;
    private readonly Action<IntPtr> _release;

    internal ForceFeedbackEffect(IntPtr effectPtr, Action<IntPtr> release)
    {
        _effectPtr = effectPtr;
        _release = release ?? throw new ArgumentNullException(nameof(release));
    }

    /// <summary>Motor index (0–7) for this effect.</summary>
    public uint MotorIndex => _effectPtr == IntPtr.Zero ? 0 : GameInputInterop.GetForceFeedbackEffectMotorIndex(_effectPtr);

    /// <summary>Current state of the effect (Stopped, Running, Paused).</summary>
    public ForceFeedbackEffectState State =>
        _effectPtr == IntPtr.Zero
            ? ForceFeedbackEffectState.Stopped
            : (ForceFeedbackEffectState)GameInputInterop.GetForceFeedbackEffectState(_effectPtr);

    /// <summary>Gets or sets the gain for this effect (0–1).</summary>
    public float Gain
    {
        get => _effectPtr == IntPtr.Zero ? 0f : GameInputInterop.GetForceFeedbackEffectGain(_effectPtr);
        set { if (_effectPtr != IntPtr.Zero) GameInputInterop.SetForceFeedbackEffectGain(_effectPtr, Math.Clamp(value, 0f, 1f)); }
    }

    /// <summary>Starts the effect (Running).</summary>
    public void Start()
    {
        if (_effectPtr != IntPtr.Zero)
            GameInputInterop.SetForceFeedbackEffectState(_effectPtr, (int)ForceFeedbackEffectState.Running);
    }

    /// <summary>Pauses the effect (Paused).</summary>
    public void Pause()
    {
        if (_effectPtr != IntPtr.Zero)
            GameInputInterop.SetForceFeedbackEffectState(_effectPtr, (int)ForceFeedbackEffectState.Paused);
    }

    /// <summary>Stops the effect (Stopped).</summary>
    public void Stop()
    {
        if (_effectPtr != IntPtr.Zero)
            GameInputInterop.SetForceFeedbackEffectState(_effectPtr, (int)ForceFeedbackEffectState.Stopped);
    }

    /// <summary>Sets the gain for this effect (0–1).</summary>
    public void SetGain(float gain)
    {
        if (_effectPtr != IntPtr.Zero)
            GameInputInterop.SetForceFeedbackEffectGain(_effectPtr, Math.Clamp(gain, 0f, 1f));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_release != null && _effectPtr != IntPtr.Zero)
        {
            _release(_effectPtr);
            _effectPtr = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
