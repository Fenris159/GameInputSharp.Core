namespace GameInputSharp.Haptics;

/// <summary>
/// Native haptic motor location. GameInput supports up to 8 locations per device.
/// </summary>
public enum HapticLocation
{
    /// <summary>Left low-frequency (rumble) motor.</summary>
    LeftLowFrequency = 0,

    /// <summary>Right high-frequency motor.</summary>
    RightHighFrequency = 1,

    /// <summary>Additional locations 2–7 for advanced controllers.</summary>
    Location2 = 2,
    Location3 = 3,
    Location4 = 4,
    Location5 = 5,
    Location6 = 6,
    Location7 = 7,
}
