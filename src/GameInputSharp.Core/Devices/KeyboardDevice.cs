using GameInputSharp.Abstractions;
using GameInputSharp.Core;

namespace GameInputSharp.Devices;

/// <summary>Wrapper for a keyboard.</summary>
/// <remarks>Dispose when done to release the native device reference.</remarks>
public sealed class KeyboardDevice : IInputDevice
{
    private IntPtr _devicePtr;
    private bool _disposed;

    internal KeyboardDevice(IntPtr devicePtr, string deviceId, string displayName)
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

    /// <summary>Gets the native device pointer (for use by <see cref="Abstractions.GameInputManager"/>).</summary>
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

    /// <summary>Gets the number of extra axes for an input kind.</summary>
    public uint GetExtraAxisCount(uint inputKind) => GameInputInterop.GetExtraAxisCount(_devicePtr, inputKind);

    /// <summary>Gets the number of extra buttons for an input kind.</summary>
    public uint GetExtraButtonCount(uint inputKind) => GameInputInterop.GetExtraButtonCount(_devicePtr, inputKind);

    /// <summary>Gets the extra axis indexes for an input kind.</summary>
    public uint[] GetExtraAxisIndexes(uint inputKind) => GameInputInterop.GetExtraAxisIndexes(_devicePtr, inputKind);

    /// <summary>Gets the extra button indexes for an input kind.</summary>
    public uint[] GetExtraButtonIndexes(uint inputKind) => GameInputInterop.GetExtraButtonIndexes(_devicePtr, inputKind);

    /// <summary>DirectInput escape. Returns (success, bytesWritten).</summary>
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

    /// <summary>Sends a raw device output report by pointer.</summary>
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

    ~KeyboardDevice()
    {
        if (_devicePtr != IntPtr.Zero)
        {
            GameInputInterop.ReleaseDevice(_devicePtr);
            _devicePtr = IntPtr.Zero;
        }
        _disposed = true;
    }
}
