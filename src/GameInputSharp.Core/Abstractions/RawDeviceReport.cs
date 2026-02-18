// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.
// Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

using System.Runtime.InteropServices;
using GameInputSharp.Core.Native;

namespace GameInputSharp.Abstractions;

/// <summary>Report info returned from <see cref="RawDeviceReport.GetReportInfo"/>.</summary>
public struct RawDeviceReportInfo
{
    /// <summary>Report kind (input or output).</summary>
    public RawDeviceReportKind Kind;

    /// <summary>Report ID.</summary>
    public uint Id;

    /// <summary>Report data size in bytes.</summary>
    public uint Size;
}

/// <summary>Kind of raw device report (matches GameInputRawDeviceReportKind).</summary>
public enum RawDeviceReportKind
{
    /// <summary>Input report.</summary>
    InputReport = 0,

    /// <summary>Output report.</summary>
    OutputReport = 1
}

/// <summary>Wraps a native raw device report (IGameInputRawDeviceReport). Create via device.CreateRawDeviceReport(); dispose when done.</summary>
/// <remarks>Exposes GetReportInfo, GetRawDataSize, GetRawData, SetRawData. Use with GamepadDevice.SendRawDeviceOutput (or the same on keyboard/mouse) to send output reports. Raw data buffer size is capped at 8192 bytes for GetRawData/SetRawData to reduce DoS (see docs/SECURITY.md).</remarks>
public sealed class RawDeviceReport : IDisposable
{
    private const int MaxRawReportDataSize = 8192;  // Security: cap to reduce DoS from huge buffers (docs/SECURITY.md)

    private IntPtr _ptr;
    private bool _disposed;

    internal RawDeviceReport(IntPtr reportPtr)
    {
        _ptr = reportPtr;
    }

    /// <summary>Native report pointer for use with IInputDevice.SendRawDeviceOutput or interop. Do not release; the wrapper owns it.</summary>
    public IntPtr UnsafePointer => _ptr;

    /// <summary>Gets the report metadata (kind, id, size).</summary>
    public bool GetReportInfo(out RawDeviceReportInfo info)
    {
        info = default;
        if (_disposed || _ptr == IntPtr.Zero)
            return false;
        try
        {
            var report = (IGameInputRawDeviceReport)Marshal.GetObjectForIUnknown(_ptr);
            try
            {
                int hr = report.GetReportInfo(out var native);
                if (hr != 0)
                    return false;
                info = new RawDeviceReportInfo
                {
                    Kind = (RawDeviceReportKind)native.Kind,
                    Id = native.Id,
                    Size = native.Size
                };
                return true;
            }
            finally
            {
                Marshal.ReleaseComObject(report);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets the raw data size in bytes.</summary>
    public uint GetRawDataSize()
    {
        if (_disposed || _ptr == IntPtr.Zero)
            return 0;
        try
        {
            var report = (IGameInputRawDeviceReport)Marshal.GetObjectForIUnknown(_ptr);
            try
            {
                ulong size = (ulong)report.GetRawDataSize();
                return size > uint.MaxValue ? uint.MaxValue : (uint)size;
            }
            finally
            {
                Marshal.ReleaseComObject(report);
            }
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Reads raw report data into the buffer. Returns the number of bytes read, or 0 on failure.</summary>
    /// <remarks>Buffer size is capped at 8192 bytes; larger buffers return 0. See docs/SECURITY.md.</remarks>
    public uint GetRawData(byte[] buffer)
    {
        if (_disposed || _ptr == IntPtr.Zero || buffer == null || buffer.Length == 0)
            return 0;
        if (buffer.Length > MaxRawReportDataSize)
            return 0;
        try
        {
            var report = (IGameInputRawDeviceReport)Marshal.GetObjectForIUnknown(_ptr);
            try
            {
                IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);
                try
                {
                    var written = report.GetRawData((UIntPtr)buffer.Length, ptr);
                    ulong writtenU = (ulong)written;
                    int n = writtenU > int.MaxValue ? buffer.Length : (int)writtenU;
                    if (n > 0 && n <= buffer.Length)
                        Marshal.Copy(ptr, buffer, 0, n);
                    return (uint)n;
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(report);
            }
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Writes raw report data from the buffer. Returns true on success.</summary>
    /// <remarks>Buffer size is capped at 8192 bytes; larger buffers return false. See docs/SECURITY.md.</remarks>
    public bool SetRawData(byte[] buffer)
    {
        if (_disposed || _ptr == IntPtr.Zero || buffer == null)
            return false;
        if (buffer.Length > MaxRawReportDataSize)
            return false;
        try
        {
            var report = (IGameInputRawDeviceReport)Marshal.GetObjectForIUnknown(_ptr);
            try
            {
                IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);
                try
                {
                    Marshal.Copy(buffer, 0, ptr, buffer.Length);
                    return report.SetRawData((UIntPtr)buffer.Length, ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(report);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        if (_ptr != IntPtr.Zero)
        {
            Marshal.Release(_ptr);
            _ptr = IntPtr.Zero;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
