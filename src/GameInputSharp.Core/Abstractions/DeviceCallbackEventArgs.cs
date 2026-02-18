namespace GameInputSharp.Abstractions;

/// <summary>Arguments for device connect/disconnect callbacks.</summary>
/// <remarks>Raised when you call <see cref="GameInputManager.DispatchCallbacks"/> after subscribing to <see cref="GameInputManager.DeviceCallback"/>. Use <see cref="DeviceId"/> to correlate with wrappers from <see cref="GameInputManager.GetDevices"/> (same value as <see cref="IInputDevice.DeviceId"/>).</remarks>
public sealed class DeviceCallbackEventArgs
{
    /// <summary>Timestamp of the status change (microseconds).</summary>
    public ulong Timestamp { get; }

    /// <summary>Current device status flags.</summary>
    public uint CurrentStatus { get; }

    /// <summary>Previous device status flags.</summary>
    public uint PreviousStatus { get; }

    /// <summary>Stable device ID (hex string). Matches <see cref="IInputDevice.DeviceId"/> so you can correlate with your device wrappers without re-enumerating.</summary>
    public string DeviceId { get; }

    internal DeviceCallbackEventArgs(ulong timestamp, uint currentStatus, uint previousStatus, string deviceId)
    {
        Timestamp = timestamp;
        CurrentStatus = currentStatus;
        PreviousStatus = previousStatus;
        DeviceId = deviceId ?? string.Empty;
    }
}
