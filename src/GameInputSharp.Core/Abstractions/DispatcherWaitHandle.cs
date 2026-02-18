// GameInputSharp.Abstractions — advanced dispatcher wait handle.
// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace GameInputSharp.Abstractions;

/// <summary>
/// Holds a GameInput dispatcher and its wait handle. Wait on <see cref="SafeWaitHandle"/> (e.g. <c>SafeWaitHandle.WaitOne()</c>) instead of polling <see cref="GameInputManager.DispatchCallbacks"/>.
/// Dispose to release the native dispatcher and close the handle.
/// </summary>
/// <remarks>
/// Created via <see cref="GameInputManager.CreateDispatcherWaitHandle"/>. The wait handle is signalled when the dispatcher has work to process.
/// Useful for threading: block a thread on the handle and call <see cref="GameInputManager.DispatchCallbacks"/> when signalled.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DispatcherWaitHandle : IDisposable
{
    private IntPtr _dispatcherPtr;
    private readonly SafeWaitHandle _waitHandle;

    internal DispatcherWaitHandle(IntPtr dispatcherPtr, IntPtr waitHandlePtr)
    {
        _dispatcherPtr = dispatcherPtr;
        _waitHandle = new SafeWaitHandle(waitHandlePtr, true);
    }

    /// <summary>OS wait handle. Use <c>WaitOne()</c> to block. Do not dispose; it is owned by this object.</summary>
    public SafeWaitHandle SafeWaitHandle => _waitHandle;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_dispatcherPtr != IntPtr.Zero)
        {
            Marshal.Release(_dispatcherPtr);
            _dispatcherPtr = IntPtr.Zero;
        }
        _waitHandle.Dispose();
        GC.SuppressFinalize(this);
    }
}
