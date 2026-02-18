using GameInputSharp.Abstractions;
using GameInputSharp.Core;
using GameInputSharp.Core.Native;

namespace GameInputSharp.Devices;

/// <summary>
/// Factory for creating high-level device wrappers (keyboard, mouse, gamepad) from native device pointers.
/// </summary>
/// <remarks>
/// Typically used by <see cref="Abstractions.GameInputManager.GetDevices"/>; call <see cref="CreateFromNative"/> only when you have a native device pointer from another source. Caller must dispose the returned device to release the native reference.
/// </remarks>
public static class DeviceFactory
{
    /// <summary>Creates an IInputDevice wrapper from a native IGameInputDevice pointer. Caller transfers ownership (Release on Dispose).</summary>
    /// <param name="devicePtr">Native device pointer from enumeration.</param>
    /// <returns>Typed device or null if kind is unsupported.</returns>
    public static IInputDevice? CreateFromNative(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero)
            return null;

        var (supportedInput, displayName) = GameInputInterop.GetDeviceInfoFromPtr(devicePtr);
        string deviceId = GameInputInterop.GetDeviceIdFromPtr(devicePtr);

        if ((supportedInput & GameInputNative.GameInputKindGamepad) != 0 || (supportedInput & GameInputNative.GameInputKindController) != 0)
            return new GamepadDevice(devicePtr, deviceId, displayName);
        if ((supportedInput & GameInputNative.GameInputKindKeyboard) != 0)
            return new KeyboardDevice(devicePtr, deviceId, displayName);
        if ((supportedInput & GameInputNative.GameInputKindMouse) != 0)
            return new MouseDevice(devicePtr, deviceId, displayName);

        // Unsupported kind: release and return null
        return null;
    }
}
