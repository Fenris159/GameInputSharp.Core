namespace GameInputSharp.Abstractions;

/// <summary>
/// High-level abstraction for an input device (gamepad, keyboard, mouse, or custom HID).
/// Implementations wrap Microsoft.GameInput IGameInputDevice.
/// </summary>
/// <remarks>
/// Obtain devices via <see cref="GameInputManager.GetDevices"/>. Dispose when done to release native references.
/// </remarks>
public interface IInputDevice : IDisposable
{
    /// <summary>Unique identifier for this device instance (stable within a session).</summary>
    string DeviceId { get; }

    /// <summary>Human-readable name (e.g. "Xbox Wireless Controller").</summary>
    string DisplayName { get; }

    /// <summary>Whether the device is currently connected and usable.</summary>
    bool IsConnected { get; }

    /// <summary>Native device pointer for advanced use (e.g. <see cref="GameInputManager.RegisterReadingCallback(IInputDevice?, uint)"/>). Do not release; the device wrapper owns it.</summary>
    IntPtr GetDevicePointer();

    /// <summary>Gets the full device info (vendor ID, product ID, firmware version, capabilities, etc.). Returns null if the native call fails.</summary>
    DeviceInfo? GetDeviceInfo();
}
