// Public DTOs for all force feedback effect kinds. Maps to GameInput structs.
// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.

namespace GameInputSharp.Haptics;

/// <summary>Kind of force feedback effect (matches GameInputForceFeedbackEffectKind).</summary>
public enum ForceFeedbackEffectKind
{
    /// <summary>Constant force for the duration.</summary>
    Constant = 0,
    /// <summary>Force ramps from start to end magnitude.</summary>
    Ramp = 1,
    /// <summary>Sine-wave oscillation.</summary>
    SineWave = 2,
    /// <summary>Square-wave oscillation.</summary>
    SquareWave = 3,
    /// <summary>Triangle-wave oscillation.</summary>
    TriangleWave = 4,
    /// <summary>Sawtooth-up wave.</summary>
    SawtoothUpWave = 5,
    /// <summary>Sawtooth-down wave.</summary>
    SawtoothDownWave = 6,
    /// <summary>Spring: force opposes displacement (e.g. wheel recenter).</summary>
    Spring = 7,
    /// <summary>Friction resistance.</summary>
    Friction = 8,
    /// <summary>Damper (velocity-based resistance).</summary>
    Damper = 9,
    /// <summary>Inertia (mass-like resistance).</summary>
    Inertia = 10
}

/// <summary>Envelope that shapes effect over time (attack, sustain, release). Durations in 100-nanosecond units (1 microsecond = 10 units).</summary>
public struct ForceFeedbackEnvelope
{
    /// <summary>Attack duration (100-ns units).</summary>
    public ulong AttackDuration;
    /// <summary>Sustain duration (100-ns units).</summary>
    public ulong SustainDuration;
    /// <summary>Release duration (100-ns units).</summary>
    public ulong ReleaseDuration;
    /// <summary>Gain during attack (0–1).</summary>
    public float AttackGain;
    /// <summary>Gain during sustain (0–1).</summary>
    public float SustainGain;
    /// <summary>Gain during release (0–1).</summary>
    public float ReleaseGain;
    /// <summary>Number of times to play (1 = once).</summary>
    public uint PlayCount;
    /// <summary>Delay between repeats (100-ns units).</summary>
    public ulong RepeatDelay;
}

/// <summary>Magnitude of force along axes. Use Normal (0–1) for simple rumble; set Linear/Angular for directional effects.</summary>
public struct ForceFeedbackMagnitude
{
    /// <summary>Linear X/Y/Z (e.g. for racing wheel).</summary>
    public float LinearX, LinearY, LinearZ;
    /// <summary>Angular X/Y/Z.</summary>
    public float AngularX, AngularY, AngularZ;
    /// <summary>Normal (single axis, 0–1). Use for simple intensity.</summary>
    public float Normal;
}

/// <summary>Parameters for a constant force effect.</summary>
public struct ForceFeedbackConstantParams
{
    /// <summary>Envelope (attack/sustain/release).</summary>
    public ForceFeedbackEnvelope Envelope;
    /// <summary>Magnitude (use Normal for simple 0–1 intensity).</summary>
    public ForceFeedbackMagnitude Magnitude;
}

/// <summary>Parameters for a ramp effect (force goes from start to end magnitude).</summary>
public struct ForceFeedbackRampParams
{
    /// <summary>Envelope.</summary>
    public ForceFeedbackEnvelope Envelope;
    /// <summary>Starting magnitude.</summary>
    public ForceFeedbackMagnitude StartMagnitude;
    /// <summary>Ending magnitude.</summary>
    public ForceFeedbackMagnitude EndMagnitude;
}

/// <summary>Parameters for periodic effects (sine, square, triangle, sawtooth).</summary>
public struct ForceFeedbackPeriodicParams
{
    /// <summary>Envelope.</summary>
    public ForceFeedbackEnvelope Envelope;
    /// <summary>Magnitude of the wave.</summary>
    public ForceFeedbackMagnitude Magnitude;
    /// <summary>Frequency in Hz.</summary>
    public float Frequency;
    /// <summary>Phase (time in cycle where playback begins), typically 0.</summary>
    public float Phase;
    /// <summary>Offset of the wave.</summary>
    public float Bias;
}

/// <summary>Parameters for condition effects (spring, friction, damper, inertia). Used for racing wheels and flight sticks.</summary>
public struct ForceFeedbackConditionParams
{
    /// <summary>Magnitude.</summary>
    public ForceFeedbackMagnitude Magnitude;
    /// <summary>Positive-direction coefficient (e.g. -1 for spring recenter).</summary>
    public float PositiveCoefficient;
    /// <summary>Negative-direction coefficient.</summary>
    public float NegativeCoefficient;
    /// <summary>Max force in positive direction.</summary>
    public float MaxPositiveMagnitude;
    /// <summary>Max force in negative direction.</summary>
    public float MaxNegativeMagnitude;
    /// <summary>Dead zone (0 = feedback immediately; 1 = no feedback).</summary>
    public float DeadZone;
    /// <summary>Bias (logical center; 0 = natural center).</summary>
    public float Bias;
}
