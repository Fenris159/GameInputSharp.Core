// GameInputSharp.Core — low-level COM interop, safe wrappers, HRESULT handling.
// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.
// Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GameInputSharp.Abstractions;
using GameInputSharp.Core.Native;
using GameInputSharp.Haptics;

namespace GameInputSharp.Core;

[SupportedOSPlatform("windows")]
internal static partial class GameInputInterop
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    private static string GetWin32ErrorMessage(int code)
    {
        return code switch
        {
            2 => "File not found.",
            5 => "Access denied.",
            126 => "A dependency of the DLL could not be found (install Visual C++ Redistributable for Visual Studio, x64).",
            127 => "A procedure specified in the DLL could not be found.",
            193 => "Wrong architecture (32-bit vs 64-bit mismatch).",
            _ => $"Win32 error {code}. See https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes"
        };
    }

    /// <summary>When DLL exists but load failed: tries LoadLibraryEx and returns the Win32 error code and message.</summary>
    public static (int Win32Error, string Message) GetLastLoadError()
    {
        if (!OperatingSystem.IsWindows())
            return (0, string.Empty);
        string systemDir = Environment.SystemDirectory ?? string.Empty;
        string path1 = string.IsNullOrEmpty(systemDir) ? "GameInput.dll" : Path.Combine(systemDir, "GameInput.dll");
        string path2 = string.IsNullOrEmpty(systemDir) ? "GameInputRedist.dll" : Path.Combine(systemDir, "GameInputRedist.dll");
        string? pathToTry = File.Exists(path1) ? path1 : (File.Exists(path2) ? path2 : null);
        if (string.IsNullOrEmpty(pathToTry))
            return (0, "No DLL found to try.");
        IntPtr h = LoadLibraryExW(pathToTry!, IntPtr.Zero, 0);
        if (h != IntPtr.Zero)
        {
            FreeLibrary(h);
            return (0, "Load succeeded (unexpected).");
        }
        int err = Marshal.GetLastWin32Error();
        return (err, GetWin32ErrorMessage(err));
    }
    private const int S_OK = 0;
    private const int E_FAIL = unchecked((int)0x80004005);
    private const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);

    // Offsets into GameInputDeviceInfo. Microsoft doc: https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/structs/gameinputdeviceinfo
    // Doc order: vendorId(2), productId(2), revisionNumber(2), usage(4), hw(8), fw(8), deviceId(32), deviceRootId(32), deviceFamily(4), supportedInput(4), supportedRumbleMotors(4), supportedSystemButtons(4), containerId(16), displayName(ptr), pnpPath(ptr)...
    // PC runtime may use 4-byte padding after deviceFamily (displayName then at 126 not 122). We use 126 for displayName so enumeration works on PC; strings are UTF-8 per docs.
    private const int OffsetVendorId = 0;
    private const int OffsetUsage = 6;
    private const int OffsetHardwareVersion = 10;
    private const int OffsetFirmwareVersion = 18;
    private const int OffsetDeviceId = 26;
    private const int OffsetDeviceRootId = 58;
    private const int OffsetDeviceFamily = 90;
    private const int OffsetSupportedInput = 98;
    private const int OffsetSupportedRumbleMotors = 102;
    private const int OffsetSupportedSystemButtons = 106;
    private const int OffsetContainerId = 110;
    private const int OffsetDisplayName = 126;   // 110+16 on PC (padding may differ from doc)
    private static int OffsetPnpPath => OffsetDisplayName + IntPtr.Size;
    private static int OffsetKeyboardInfo => OffsetPnpPath + IntPtr.Size;  // after pnpPath pointer
    private static int OffsetMouseInfo => OffsetKeyboardInfo + IntPtr.Size;
    private static int OffsetSensorsInfo => OffsetMouseInfo + IntPtr.Size;
    private static int OffsetControllerInfo => OffsetSensorsInfo + IntPtr.Size;
    private static int OffsetArcadeStickInfo => OffsetControllerInfo + IntPtr.Size;
    private static int OffsetFlightStickInfo => OffsetArcadeStickInfo + IntPtr.Size;
    private static int OffsetGamepadInfo => OffsetFlightStickInfo + IntPtr.Size;
    private static int OffsetRacingWheelInfo => OffsetGamepadInfo + IntPtr.Size;
    private static int OffsetForceFeedbackMotorCount => OffsetRacingWheelInfo + IntPtr.Size;
    private static int OffsetForceFeedbackMotorInfo => OffsetForceFeedbackMotorCount + 4;
    private static int OffsetInputReportCount => OffsetForceFeedbackMotorInfo + IntPtr.Size;
    private static int OffsetInputReportInfo => OffsetInputReportCount + 4;
    private static int OffsetOutputReportCount => OffsetInputReportInfo + IntPtr.Size;
    private static int OffsetOutputReportInfo => OffsetOutputReportCount + 4;

    // Security hardening: limits to reduce DoS from unbounded allocations or native abuse (see docs/SECURITY.md).
    private const int MaxExtraAxisOrButtonCount = 1024;
    private const int MaxDirectInputEscapeBufferSize = 65536;  // 64 KB per buffer
    private const int MaxPlatformStringLength = 2048;

    /// <summary>Diagnostic: paths checked for GameInput DLL and process bitness. Use when DLL fails to load to verify 32/64-bit and path.</summary>
    public static (string GameInputDllPath, string GameInputRedistPath, bool GameInputDllExists, bool GameInputRedistExists, bool Is64BitProcess) GetLoadPaths()
    {
        if (!OperatingSystem.IsWindows())
            return (string.Empty, string.Empty, false, false, Environment.Is64BitProcess);
        string systemDir = Environment.SystemDirectory ?? string.Empty;
        string path1 = string.IsNullOrEmpty(systemDir) ? "GameInput.dll" : Path.Combine(systemDir, "GameInput.dll");
        string path2 = string.IsNullOrEmpty(systemDir) ? "GameInputRedist.dll" : Path.Combine(systemDir, "GameInputRedist.dll");
        return (path1, path2, File.Exists(path1), File.Exists(path2), Environment.Is64BitProcess);
    }

    /// <summary>Diagnostic: reports whether the GameInput DLL loaded and the HRESULT from GameInputCreate. Does not retain the COM object.</summary>
    /// <returns>DllLoaded true if the native DLL was loaded; InitHResult is 0 on success, or the HRESULT from GameInputCreate, or -1 if the DLL has no GameInputCreate export.</returns>
    public static (bool DllLoaded, int InitHResult) GetLoadDiagnostics()
    {
        try
        {
            if (!TryLoadGameInputDll(out IntPtr dllHandle))
                return (false, 0);

            IntPtr createPtr = NativeLibrary.GetExport(dllHandle, "GameInputCreate");
            if (createPtr == IntPtr.Zero)
                return (true, -1);

            var create = Marshal.GetDelegateForFunctionPointer<GameInputCreateDelegate>(createPtr);
            int hr = create(out IntPtr ppv);
            if (ppv != IntPtr.Zero)
                Marshal.Release(ppv);
            return (true, hr);
        }
        catch
        {
            return (false, 0);
        }
    }

    /// <summary>Attempts to create the GameInput singleton via GameInputCreate (official API). Returns null if DLL not found or init fails.</summary>
    /// <param name="loadOnlyFromSystem32">If true, load the DLL only from System32 (no app directory or default path). Use for maximum security; see docs/SECURITY.md.</param>
    public static IGameInput? TryCreateGameInput(bool loadOnlyFromSystem32 = false)
    {
        try
        {
            IntPtr dllHandle;
            if (loadOnlyFromSystem32)
            {
                if (!TryLoadGameInputDllFromSystem32Only(out dllHandle))
                    return null;
            }
            else
            {
                if (!TryLoadGameInputDll(out dllHandle))
                    return null;
            }

            IntPtr createPtr = NativeLibrary.GetExport(dllHandle, "GameInputCreate");
            if (createPtr == IntPtr.Zero)
                return null;

            var create = Marshal.GetDelegateForFunctionPointer<GameInputCreateDelegate>(createPtr);
            int hr = create(out IntPtr ppv);
            if (hr != S_OK || ppv == IntPtr.Zero)
                return null;

            try
            {
                return (IGameInput)Marshal.GetObjectForIUnknown(ppv);
            }
            finally
            {
                Marshal.Release(ppv);
            }
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (InvalidCastException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>Enumerates connected devices via blocking device callback. Returns list of raw device pointers (caller must release).</summary>
    /// <remarks>Uses device kind Gamepad | Controller | Keyboard | Mouse so all common devices are enumerated (matching behavior of apps like EDForceFeedbackXinput that use GameInputKindGamepad).</remarks>
    public static List<IntPtr> EnumerateDevices(IGameInput gameInput)
    {
        var devicePtrs = new List<IntPtr>();
        uint deviceKind = GameInputNative.GameInputKindGamepad | GameInputNative.GameInputKindController
            | GameInputNative.GameInputKindKeyboard | GameInputNative.GameInputKindMouse;
        var callback = new GameInputDeviceCallbackDelegate(DeviceCallback);
        var contextHandle = GCHandle.Alloc(devicePtrs);

        try
        {
            int hr = gameInput.RegisterDeviceCallback(
                IntPtr.Zero,
                deviceKind,
                GameInputNative.GameInputDeviceConnected,
                GameInputNative.GameInputBlockingEnumeration,
                GCHandle.ToIntPtr(contextHandle),
                Marshal.GetFunctionPointerForDelegate(callback),
                out ulong token);

            if (hr != S_OK)
                return devicePtrs;

            // Create dispatcher and run it to pump the enumeration callbacks
            hr = gameInput.CreateDispatcher(out IntPtr dispatcherPtr);
            if (hr == S_OK && dispatcherPtr != IntPtr.Zero)
            {
                try
                {
                    var dispatcher = (IGameInputDispatcher)Marshal.GetObjectForIUnknown(dispatcherPtr);
                    dispatcher.Dispatch(1_000_000); // 1 second quota
                }
                finally
                {
                    Marshal.Release(dispatcherPtr);
                }
            }

            gameInput.StopCallback(token);
        }
        finally
        {
            contextHandle.Free();
        }

        return devicePtrs;
    }

    private static void DeviceCallback(ulong callbackToken, IntPtr context, IntPtr device, ulong timestamp, uint currentStatus, uint previousStatus)
    {
        if (device == IntPtr.Zero || context == IntPtr.Zero)
            return;
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is List<IntPtr> list)
        {
            Marshal.AddRef(device);
            list.Add(device);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void GameInputDeviceCallbackDelegate(ulong callbackToken, IntPtr context, IntPtr device, ulong timestamp, uint currentStatus, uint previousStatus);

    private static readonly GameInputDeviceCallbackDelegate s_deviceEventCallback = DeviceCallbackForEvents;

    private static void DeviceCallbackForEvents(ulong callbackToken, IntPtr context, IntPtr device, ulong timestamp, uint currentStatus, uint previousStatus)
    {
        if (context == IntPtr.Zero) return;
        try
        {
            var handle = GCHandle.FromIntPtr(context);
            string deviceId = device != IntPtr.Zero ? GetDeviceIdFromPtr(device) : string.Empty;
            if (handle.Target is System.Collections.Concurrent.ConcurrentQueue<(ulong, uint, uint, string)> queue)
                queue.Enqueue((timestamp, currentStatus, previousStatus, deviceId));
        }
        catch { /* ignore */ }
    }

    /// <summary>Registers for async device callbacks (connect/disconnect).</summary>
    /// <param name="gameInput">IGameInput instance.</param>
    /// <param name="contextHandle">GCHandle to a ConcurrentQueue of (timestamp, currentStatus, previousStatus, deviceId) tuples.</param>
    /// <param name="token">Receives the callback token for UnregisterDeviceCallback.</param>
    /// <returns>True if registration succeeded.</returns>
    public static bool RegisterDeviceCallbackAsync(IGameInput gameInput, GCHandle contextHandle, out ulong token)
    {
        token = 0;
        if (gameInput == null || !contextHandle.IsAllocated)
            return false;
        int hr = gameInput.RegisterDeviceCallback(
            IntPtr.Zero,
            GameInputNative.GameInputKindUnknown,
            GameInputNative.GameInputDeviceAnyStatus,
            GameInputNative.GameInputAsyncEnumeration,
            GCHandle.ToIntPtr(contextHandle),
            Marshal.GetFunctionPointerForDelegate(s_deviceEventCallback),
            out token);
        return hr == S_OK;
    }

    /// <summary>Unregisters a device callback.</summary>
    public static bool UnregisterDeviceCallback(IGameInput gameInput, ulong token)
    {
        if (gameInput == null) return false;
        return gameInput.UnregisterCallback(token);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void GameInputReadingCallbackDelegate(ulong callbackToken, IntPtr context, IntPtr reading, [MarshalAs(UnmanagedType.I1)] bool hasOverrunOccurred);

    private static readonly GameInputReadingCallbackDelegate s_readingCallback = ReadingCallbackForEvents;

    private static void ReadingCallbackForEvents(ulong callbackToken, IntPtr context, IntPtr reading, bool hasOverrunOccurred)
    {
        if (context == IntPtr.Zero || reading == IntPtr.Zero) return;
        try
        {
            Marshal.AddRef(reading);
            var handle = GCHandle.FromIntPtr(context);
            if (handle.Target is System.Collections.Concurrent.ConcurrentQueue<(IntPtr, bool)> queue)
                queue.Enqueue((reading, hasOverrunOccurred));
        }
        catch { /* ignore */ }
    }

    /// <summary>Registers for reading callbacks (new input readings). Context = GCHandle to ConcurrentQueue of (IntPtr reading, bool hasOverrun). Caller must AddRef reading when enqueueing; we do it in our static callback.</summary>
    public static bool RegisterReadingCallback(IGameInput gameInput, IntPtr devicePtr, uint inputKind, GCHandle contextHandle, out ulong token)
    {
        token = 0;
        if (gameInput == null || !contextHandle.IsAllocated)
            return false;
        int hr = gameInput.RegisterReadingCallback(devicePtr, inputKind, GCHandle.ToIntPtr(contextHandle), Marshal.GetFunctionPointerForDelegate(s_readingCallback), out token);
        return hr == S_OK;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void GameInputSystemButtonCallbackDelegate(ulong callbackToken, IntPtr context);

    private static readonly GameInputSystemButtonCallbackDelegate s_systemButtonCallback = SystemButtonCallbackForEvents;

    private static void SystemButtonCallbackForEvents(ulong callbackToken, IntPtr context)
    {
        if (context == IntPtr.Zero) return;
        try
        {
            var handle = GCHandle.FromIntPtr(context);
            if (handle.Target is System.Collections.Concurrent.ConcurrentQueue<object> queue)
                queue.Enqueue(null!); // signal that callback fired
        }
        catch { /* ignore */ }
    }

    /// <summary>Registers for system button (e.g. Xbox) callbacks. Context = GCHandle to ConcurrentQueue (we enqueue a sentinel when fired).</summary>
    public static bool RegisterSystemButtonCallback(IGameInput gameInput, IntPtr devicePtr, uint buttonFilter, GCHandle contextHandle, out ulong token)
    {
        token = 0;
        if (gameInput == null || !contextHandle.IsAllocated)
            return false;
        int hr = gameInput.RegisterSystemButtonCallback(devicePtr, buttonFilter, GCHandle.ToIntPtr(contextHandle), Marshal.GetFunctionPointerForDelegate(s_systemButtonCallback), out token);
        return hr == S_OK;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void GameInputKeyboardLayoutCallbackDelegate(ulong callbackToken, IntPtr context);

    private static readonly GameInputKeyboardLayoutCallbackDelegate s_keyboardLayoutCallback = KeyboardLayoutCallbackForEvents;

    private static void KeyboardLayoutCallbackForEvents(ulong callbackToken, IntPtr context)
    {
        if (context == IntPtr.Zero) return;
        try
        {
            var handle = GCHandle.FromIntPtr(context);
            if (handle.Target is System.Collections.Concurrent.ConcurrentQueue<object> queue)
                queue.Enqueue(null!);
        }
        catch { /* ignore */ }
    }

    /// <summary>Registers for keyboard layout change callbacks. Context = GCHandle to ConcurrentQueue (we enqueue a sentinel when fired).</summary>
    public static bool RegisterKeyboardLayoutCallback(IGameInput gameInput, IntPtr devicePtr, GCHandle contextHandle, out ulong token)
    {
        token = 0;
        if (gameInput == null || !contextHandle.IsAllocated)
            return false;
        int hr = gameInput.RegisterKeyboardLayoutCallback(devicePtr, GCHandle.ToIntPtr(contextHandle), Marshal.GetFunctionPointerForDelegate(s_keyboardLayoutCallback), out token);
        return hr == S_OK;
    }

    /// <summary>Unregisters any callback by token.</summary>
    public static bool UnregisterCallback(IGameInput gameInput, ulong token)
    {
        if (gameInput == null) return false;
        return gameInput.UnregisterCallback(token);
    }

    /// <summary>Dispatches pending callbacks (call from game loop).</summary>
    /// <param name="gameInput">IGameInput instance.</param>
    /// <param name="quotaMicroseconds">Max time to spend dispatching.</param>
    /// <returns>True if dispatcher ran.</returns>
    public static bool DispatchCallbacks(IGameInput gameInput, ulong quotaMicroseconds = 1000)
    {
        if (gameInput == null) return false;
        int hr = gameInput.CreateDispatcher(out IntPtr dispatcherPtr);
        if (hr != S_OK || dispatcherPtr == IntPtr.Zero)
            return false;
        try
        {
            var dispatcher = (IGameInputDispatcher)Marshal.GetObjectForIUnknown(dispatcherPtr);
            return dispatcher.Dispatch(quotaMicroseconds);
        }
        finally
        {
            Marshal.Release(dispatcherPtr);
        }
    }

    /// <summary>Creates a dispatcher and its wait handle. Caller must release dispatcher and close wait handle when done.</summary>
    /// <param name="gameInput">IGameInput instance.</param>
    /// <param name="dispatcherPtr">Receives the dispatcher pointer (call Marshal.Release when done).</param>
    /// <param name="waitHandlePtr">Receives the OS wait handle (close with SafeWaitHandle or CloseHandle when done).</param>
    /// <returns>True if both were created.</returns>
    public static bool CreateDispatcherWaitHandle(IGameInput gameInput, out IntPtr dispatcherPtr, out IntPtr waitHandlePtr)
    {
        dispatcherPtr = IntPtr.Zero;
        waitHandlePtr = IntPtr.Zero;
        if (gameInput == null) return false;
        int hr = gameInput.CreateDispatcher(out dispatcherPtr);
        if (hr != S_OK || dispatcherPtr == IntPtr.Zero) return false;
        try
        {
            var dispatcher = (IGameInputDispatcher)Marshal.GetObjectForIUnknown(dispatcherPtr);
            hr = dispatcher.OpenWaitHandle(out waitHandlePtr);
            if (hr != S_OK || waitHandlePtr == IntPtr.Zero)
            {
                Marshal.Release(dispatcherPtr);
                dispatcherPtr = IntPtr.Zero;
                return false;
            }
            return true;
        }
        catch
        {
            Marshal.Release(dispatcherPtr);
            dispatcherPtr = IntPtr.Zero;
            return false;
        }
    }

    /// <summary>Stops a callback without waiting for in-flight callbacks. Use <see cref="UnregisterCallback"/> to wait for completion.</summary>
    public static void StopCallback(IGameInput gameInput, ulong callbackToken)
    {
        if (gameInput == null) return;
        try { gameInput.StopCallback(callbackToken); }
        catch { /* ignore */ }
    }

    private delegate int GameInputCreateDelegate(out IntPtr ppv);

    private static int _lastTryLoadWin32Error;
    private static string? _lastTryLoadException;

    /// <summary>After init fails with DLL loaded false: returns the Win32 error and/or exception from the main load path (TryLoadByPath). Use to see why the main path failed while GetLastLoadError may succeed when called later.</summary>
    public static (int Win32Error, string? ExceptionMessage) GetMainPathLoadFailure()
    {
        int err = _lastTryLoadWin32Error;
        string? ex = _lastTryLoadException;
        _lastTryLoadWin32Error = 0;
        _lastTryLoadException = null;
        return (err, ex);
    }

    private static bool TryLoadByPath(string fullPath, out IntPtr handle)
    {
        handle = IntPtr.Zero;
        _lastTryLoadWin32Error = 0;
        _lastTryLoadException = null;
        if (string.IsNullOrEmpty(fullPath))
            return false;
        try
        {
            if (!File.Exists(fullPath))
            {
                _lastTryLoadException = $"File.Exists false: {fullPath}";
                return false;
            }
            if (OperatingSystem.IsWindows())
            {
                handle = LoadLibraryExW(fullPath, IntPtr.Zero, 0);
                if (handle != IntPtr.Zero)
                    return true;
                _lastTryLoadWin32Error = Marshal.GetLastWin32Error();
                return false;
            }
            return NativeLibrary.TryLoad(fullPath, out handle);
        }
        catch (Exception ex)
        {
            _lastTryLoadException = ex.Message;
            return false;
        }
    }

    private static bool TryLoadGameInputDll(out IntPtr handle)
    {
        handle = IntPtr.Zero;
        if (!OperatingSystem.IsWindows())
            return false;

        // 1) System32 first (same path/order as diagnostic that succeeds on this machine)
        if (TryLoadGameInputDllFromSystem32Only(out handle))
            return true;

        // 2) Application directory
        string? appDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appDir))
        {
            if (TryLoadByPath(Path.Combine(appDir, "GameInput.dll"), out handle))
                return true;
            if (TryLoadByPath(Path.Combine(appDir, "GameInputRedist.dll"), out handle))
                return true;
        }

        // 3) Default search path (no full path)
        if (NativeLibrary.TryLoad("GameInput.dll", out handle))
            return true;

        return false;
    }

    /// <summary>Loads GameInput DLL only from System32 (no app directory or default path). Use for maximum security to avoid DLL hijacking. See docs/SECURITY.md.</summary>
    /// <returns>True if GameInput.dll or GameInputRedist.dll was loaded from System32.</returns>
    public static bool TryLoadGameInputDllFromSystem32Only(out IntPtr handle)
    {
        handle = IntPtr.Zero;
        if (!OperatingSystem.IsWindows())
            return false;
        string? systemDir = Environment.SystemDirectory;
        if (string.IsNullOrEmpty(systemDir))
            return false;
        if (TryLoadByPath(Path.Combine(systemDir, "GameInput.dll"), out handle))
            return true;
        if (TryLoadByPath(Path.Combine(systemDir, "GameInputRedist.dll"), out handle))
            return true;
        return false;
    }

    /// <summary>Gets device info from a raw device pointer. Returns (supportedInputKind, displayName).</summary>
    public static (uint SupportedInput, string DisplayName) GetDeviceInfoFromPtr(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero)
            return (0, string.Empty);

        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            int hr = device.GetDeviceInfo(out IntPtr infoPtr);
            if (hr != S_OK || infoPtr == IntPtr.Zero)
                return (0, string.Empty);

            uint supportedInput = (uint)Marshal.ReadInt32(infoPtr, OffsetSupportedInput);
            // Display name: per Microsoft docs strings are UTF-8. On PC, reading the displayName pointer can cause
            // AccessViolation (layout or memory protection may differ). Return empty so enumeration always succeeds.
            return (supportedInput, string.Empty);
        }
        catch
        {
            return (0, string.Empty);
        }
    }

    /// <summary>Gets a stable device ID string from the device (for IInputDevice.DeviceId).</summary>
    public static string GetDeviceIdFromPtr(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero)
            return string.Empty;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            int hr = device.GetDeviceInfo(out IntPtr infoPtr);
            if (hr != S_OK || infoPtr == IntPtr.Zero)
                return string.Empty;
            // Use deviceId bytes as base for a string ID
            var idBytes = new byte[Native.AppLocalDeviceId.Size];
            Marshal.Copy(IntPtr.Add(infoPtr, 26), idBytes, 0, Native.AppLocalDeviceId.Size);
            return Convert.ToHexString(idBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Gets the full device info struct from a device pointer. Returns null if the device or GetDeviceInfo fails.</summary>
    public static DeviceInfo? GetFullDeviceInfo(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero)
            return null;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            int hr = device.GetDeviceInfo(out IntPtr infoPtr);
            if (hr != S_OK || infoPtr == IntPtr.Zero)
                return null;

            ushort vendorId = (ushort)Marshal.ReadInt16(infoPtr, OffsetVendorId);
            ushort productId = (ushort)Marshal.ReadInt16(infoPtr, OffsetVendorId + 2);
            ushort revisionNumber = (ushort)Marshal.ReadInt16(infoPtr, OffsetVendorId + 4);
            ushort usagePage = (ushort)Marshal.ReadInt16(infoPtr, OffsetUsage);
            ushort usageId = (ushort)Marshal.ReadInt16(infoPtr, OffsetUsage + 2);
            var usage = new GameInputUsage { Page = usagePage, Id = usageId };

            var hw = new GameInputVersion
            {
                Major = (ushort)Marshal.ReadInt16(infoPtr, OffsetHardwareVersion),
                Minor = (ushort)Marshal.ReadInt16(infoPtr, OffsetHardwareVersion + 2),
                Build = (ushort)Marshal.ReadInt16(infoPtr, OffsetHardwareVersion + 4),
                Revision = (ushort)Marshal.ReadInt16(infoPtr, OffsetHardwareVersion + 6)
            };
            var fw = new GameInputVersion
            {
                Major = (ushort)Marshal.ReadInt16(infoPtr, OffsetFirmwareVersion),
                Minor = (ushort)Marshal.ReadInt16(infoPtr, OffsetFirmwareVersion + 2),
                Build = (ushort)Marshal.ReadInt16(infoPtr, OffsetFirmwareVersion + 4),
                Revision = (ushort)Marshal.ReadInt16(infoPtr, OffsetFirmwareVersion + 6)
            };

            var deviceIdBytes = new byte[Native.AppLocalDeviceId.Size];
            Marshal.Copy(IntPtr.Add(infoPtr, OffsetDeviceId), deviceIdBytes, 0, deviceIdBytes.Length);
            var deviceRootIdBytes = new byte[Native.AppLocalDeviceId.Size];
            Marshal.Copy(IntPtr.Add(infoPtr, OffsetDeviceRootId), deviceRootIdBytes, 0, deviceRootIdBytes.Length);

            uint deviceFamily = (uint)Marshal.ReadInt32(infoPtr, OffsetDeviceFamily);
            uint supportedInput = (uint)Marshal.ReadInt32(infoPtr, OffsetSupportedInput);
            uint supportedRumbleMotors = (uint)Marshal.ReadInt32(infoPtr, OffsetSupportedRumbleMotors);
            uint supportedSystemButtons = (uint)Marshal.ReadInt32(infoPtr, OffsetSupportedSystemButtons);

            var containerIdBytes = new byte[16];
            Marshal.Copy(IntPtr.Add(infoPtr, OffsetContainerId), containerIdBytes, 0, 16);
            var containerId = new Guid(containerIdBytes);

            // displayName and pnpPath are const char* (UTF-8) per Microsoft docs. Reading them on PC can cause
            // AccessViolation (uncatchable); use empty until a safe read is available (e.g. native probe).
            string displayName = string.Empty;
            string pnpPath = string.Empty;

            bool hasKeyboardInfo = Marshal.ReadIntPtr(infoPtr, OffsetKeyboardInfo) != IntPtr.Zero;
            bool hasMouseInfo = Marshal.ReadIntPtr(infoPtr, OffsetMouseInfo) != IntPtr.Zero;
            bool hasSensorsInfo = Marshal.ReadIntPtr(infoPtr, OffsetSensorsInfo) != IntPtr.Zero;
            bool hasControllerInfo = Marshal.ReadIntPtr(infoPtr, OffsetControllerInfo) != IntPtr.Zero;
            bool hasArcadeStickInfo = Marshal.ReadIntPtr(infoPtr, OffsetArcadeStickInfo) != IntPtr.Zero;
            bool hasFlightStickInfo = Marshal.ReadIntPtr(infoPtr, OffsetFlightStickInfo) != IntPtr.Zero;
            bool hasGamepadInfo = Marshal.ReadIntPtr(infoPtr, OffsetGamepadInfo) != IntPtr.Zero;
            bool hasRacingWheelInfo = Marshal.ReadIntPtr(infoPtr, OffsetRacingWheelInfo) != IntPtr.Zero;

            uint forceFeedbackMotorCount = (uint)Marshal.ReadInt32(infoPtr, OffsetForceFeedbackMotorCount);
            bool hasForceFeedbackMotorInfo = Marshal.ReadIntPtr(infoPtr, OffsetForceFeedbackMotorInfo) != IntPtr.Zero;
            uint inputReportCount = (uint)Marshal.ReadInt32(infoPtr, OffsetInputReportCount);
            bool hasInputReportInfo = Marshal.ReadIntPtr(infoPtr, OffsetInputReportInfo) != IntPtr.Zero;
            uint outputReportCount = (uint)Marshal.ReadInt32(infoPtr, OffsetOutputReportCount);
            bool hasOutputReportInfo = Marshal.ReadIntPtr(infoPtr, OffsetOutputReportInfo) != IntPtr.Zero;

            return new DeviceInfo
            {
                VendorId = vendorId,
                ProductId = productId,
                RevisionNumber = revisionNumber,
                Usage = usage,
                HardwareVersion = hw,
                FirmwareVersion = fw,
                DeviceId = deviceIdBytes,
                DeviceRootId = deviceRootIdBytes,
                DeviceFamily = deviceFamily,
                SupportedInput = supportedInput,
                SupportedRumbleMotors = supportedRumbleMotors,
                SupportedSystemButtons = supportedSystemButtons,
                ContainerId = containerId,
                DisplayName = displayName,
                PnpPath = pnpPath,
                HasKeyboardInfo = hasKeyboardInfo,
                HasMouseInfo = hasMouseInfo,
                HasSensorsInfo = hasSensorsInfo,
                HasControllerInfo = hasControllerInfo,
                HasArcadeStickInfo = hasArcadeStickInfo,
                HasFlightStickInfo = hasFlightStickInfo,
                HasGamepadInfo = hasGamepadInfo,
                HasRacingWheelInfo = hasRacingWheelInfo,
                ForceFeedbackMotorCount = forceFeedbackMotorCount,
                HasForceFeedbackMotorInfo = hasForceFeedbackMotorInfo,
                InputReportCount = inputReportCount,
                HasInputReportInfo = hasInputReportInfo,
                OutputReportCount = outputReportCount,
                HasOutputReportInfo = hasOutputReportInfo
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Reads a null-terminated UTF-8 string from native memory (GameInputDeviceInfo strings are UTF-8 per Microsoft docs).</summary>
    private static string PtrToUtf8(IntPtr ptr, int maxByteLength = 512)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;
        int len = 0;
        while (len < maxByteLength && Marshal.ReadByte(ptr, len) != 0)
            len++;
        if (len == 0)
            return string.Empty;
        var bytes = new byte[len];
        Marshal.Copy(ptr, bytes, 0, len);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Plays a constant force feedback effect on a motor. Fire-and-forget; for granular control use CreateForceFeedbackEffectConstantRetain and wrap in ForceFeedbackEffect.</summary>
    /// <param name="devicePtr">Native device pointer.</param>
    /// <param name="motorIndex">Motor index 0–7.</param>
    /// <param name="durationMicroseconds">Sustain duration in microseconds.</param>
    /// <param name="intensity">Magnitude 0–1 (normal axis).</param>
    /// <returns>True if effect was started.</returns>
    public static bool PlayForceFeedbackConstant(IntPtr devicePtr, int motorIndex, ulong durationMicroseconds, float intensity)
    {
        IntPtr effectPtr = CreateForceFeedbackEffectConstantRetain(devicePtr, motorIndex, durationMicroseconds, intensity);
        if (effectPtr == IntPtr.Zero) return false;
        try
        {
            var effect = (Native.IGameInputForceFeedbackEffect)Marshal.GetObjectForIUnknown(effectPtr);
            effect.SetState((int)Native.GameInputFeedbackEffectState.Running);
            return true;
        }
        finally
        {
            Marshal.Release(effectPtr);
        }
    }

    /// <summary>Creates a constant force feedback effect and returns the effect pointer (caller must release or wrap in ForceFeedbackEffect). Effect is not started; call SetState(Running) or use the wrapper.</summary>
    public static IntPtr CreateForceFeedbackEffectConstantRetain(IntPtr devicePtr, int motorIndex, ulong durationMicroseconds, float intensity)
    {
        var envelope = new Native.GameInputForceFeedbackEnvelope
        {
            AttackDuration = 0,
            SustainDuration = durationMicroseconds * 10,
            ReleaseDuration = 0,
            AttackGain = 1f,
            SustainGain = 1f,
            ReleaseGain = 0f,
            PlayCount = 1,
            RepeatDelay = 0
        };
        var magnitude = new Native.GameInputForceFeedbackMagnitude
        {
            LinearX = 0, LinearY = 0, LinearZ = 0,
            AngularX = 0, AngularY = 0, AngularZ = 0,
            Normal = Math.Clamp(intensity, 0f, 1f)
        };
        var p = new Native.GameInputForceFeedbackParamsUnion
        {
            Kind = (int)Native.GameInputForceFeedbackEffectKind.Constant,
            Constant = new Native.GameInputForceFeedbackConstantParams { Envelope = envelope, Magnitude = magnitude }
        };
        return CreateForceFeedbackEffectFromParams(devicePtr, motorIndex, ref p);
    }

    /// <summary>Creates a force feedback effect from full params (any kind). Caller must release or wrap in ForceFeedbackEffect.</summary>
    public static IntPtr CreateForceFeedbackEffectFromParams(IntPtr devicePtr, int motorIndex, ref Native.GameInputForceFeedbackParamsUnion paramsUnion)
    {
        if (devicePtr == IntPtr.Zero || motorIndex < 0 || motorIndex > 7)
            return IntPtr.Zero;
        try
        {
            var device = (Native.IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            int hr = device.CreateForceFeedbackEffect((uint)motorIndex, in paramsUnion, out IntPtr effectPtr);
            if (hr != S_OK || effectPtr == IntPtr.Zero) return IntPtr.Zero;
            var effect = (Native.IGameInputForceFeedbackEffect)Marshal.GetObjectForIUnknown(effectPtr);
            effect.SetParams(in paramsUnion);
            return effectPtr;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>Creates a force feedback effect from public constant params. Caller must release or wrap in ForceFeedbackEffect.</summary>
    public static IntPtr CreateForceFeedbackEffectFromConstant(IntPtr devicePtr, int motorIndex, in ForceFeedbackConstantParams p)
    {
        var union = ToNativeConstant(p);
        return CreateForceFeedbackEffectFromParams(devicePtr, motorIndex, ref union);
    }

    /// <summary>Creates a force feedback effect from public ramp params.</summary>
    public static IntPtr CreateForceFeedbackEffectFromRamp(IntPtr devicePtr, int motorIndex, in ForceFeedbackRampParams p)
    {
        var union = ToNativeRamp(p);
        return CreateForceFeedbackEffectFromParams(devicePtr, motorIndex, ref union);
    }

    /// <summary>Creates a force feedback effect from public periodic params (sine, square, triangle, sawtooth).</summary>
    public static IntPtr CreateForceFeedbackEffectFromPeriodic(IntPtr devicePtr, int motorIndex, ForceFeedbackEffectKind kind, in ForceFeedbackPeriodicParams p)
    {
        if (kind is < ForceFeedbackEffectKind.SineWave or > ForceFeedbackEffectKind.SawtoothDownWave)
            return IntPtr.Zero;
        var union = ToNativePeriodic(kind, p);
        return CreateForceFeedbackEffectFromParams(devicePtr, motorIndex, ref union);
    }

    /// <summary>Creates a force feedback effect from public condition params (spring, friction, damper, inertia).</summary>
    public static IntPtr CreateForceFeedbackEffectFromCondition(IntPtr devicePtr, int motorIndex, ForceFeedbackEffectKind kind, in ForceFeedbackConditionParams p)
    {
        if (kind is < ForceFeedbackEffectKind.Spring or > ForceFeedbackEffectKind.Inertia)
            return IntPtr.Zero;
        var union = ToNativeCondition(kind, p);
        return CreateForceFeedbackEffectFromParams(devicePtr, motorIndex, ref union);
    }

    private static Native.GameInputForceFeedbackParamsUnion ToNativeConstant(in ForceFeedbackConstantParams p)
    {
        return new Native.GameInputForceFeedbackParamsUnion
        {
            Kind = (int)Native.GameInputForceFeedbackEffectKind.Constant,
            Constant = new Native.GameInputForceFeedbackConstantParams
            {
                Envelope = ToNativeEnvelope(p.Envelope),
                Magnitude = ToNativeMagnitude(p.Magnitude)
            }
        };
    }

    private static Native.GameInputForceFeedbackParamsUnion ToNativeRamp(in ForceFeedbackRampParams p)
    {
        return new Native.GameInputForceFeedbackParamsUnion
        {
            Kind = (int)Native.GameInputForceFeedbackEffectKind.Ramp,
            Ramp = new Native.GameInputForceFeedbackRampParams
            {
                Envelope = ToNativeEnvelope(p.Envelope),
                StartMagnitude = ToNativeMagnitude(p.StartMagnitude),
                EndMagnitude = ToNativeMagnitude(p.EndMagnitude)
            }
        };
    }

    private static Native.GameInputForceFeedbackParamsUnion ToNativePeriodic(ForceFeedbackEffectKind kind, in ForceFeedbackPeriodicParams p)
    {
        return new Native.GameInputForceFeedbackParamsUnion
        {
            Kind = (int)kind,
            Periodic = new Native.GameInputForceFeedbackPeriodicParams
            {
                Envelope = ToNativeEnvelope(p.Envelope),
                Magnitude = ToNativeMagnitude(p.Magnitude),
                Frequency = p.Frequency,
                Phase = p.Phase,
                Bias = p.Bias
            }
        };
    }

    private static Native.GameInputForceFeedbackParamsUnion ToNativeCondition(ForceFeedbackEffectKind kind, in ForceFeedbackConditionParams p)
    {
        return new Native.GameInputForceFeedbackParamsUnion
        {
            Kind = (int)kind,
            Condition = new Native.GameInputForceFeedbackConditionParams
            {
                Magnitude = ToNativeMagnitude(p.Magnitude),
                PositiveCoefficient = p.PositiveCoefficient,
                NegativeCoefficient = p.NegativeCoefficient,
                MaxPositiveMagnitude = p.MaxPositiveMagnitude,
                MaxNegativeMagnitude = p.MaxNegativeMagnitude,
                DeadZone = p.DeadZone,
                Bias = p.Bias
            }
        };
    }

    private static Native.GameInputForceFeedbackEnvelope ToNativeEnvelope(ForceFeedbackEnvelope e)
    {
        return new Native.GameInputForceFeedbackEnvelope
        {
            AttackDuration = e.AttackDuration,
            SustainDuration = e.SustainDuration,
            ReleaseDuration = e.ReleaseDuration,
            AttackGain = e.AttackGain,
            SustainGain = e.SustainGain,
            ReleaseGain = e.ReleaseGain,
            PlayCount = e.PlayCount,
            RepeatDelay = e.RepeatDelay
        };
    }

    private static Native.GameInputForceFeedbackMagnitude ToNativeMagnitude(ForceFeedbackMagnitude m)
    {
        return new Native.GameInputForceFeedbackMagnitude
        {
            LinearX = m.LinearX, LinearY = m.LinearY, LinearZ = m.LinearZ,
            AngularX = m.AngularX, AngularY = m.AngularY, AngularZ = m.AngularZ,
            Normal = m.Normal
        };
    }

    /// <summary>Gets the current state of a force feedback effect (Stopped=0, Running=1, Paused=2).</summary>
    public static int GetForceFeedbackEffectState(IntPtr effectPtr)
    {
        if (effectPtr == IntPtr.Zero) return (int)Native.GameInputFeedbackEffectState.Stopped;
        try
        {
            var effect = (Native.IGameInputForceFeedbackEffect)Marshal.GetObjectForIUnknown(effectPtr);
            return effect.GetState();
        }
        catch { return (int)Native.GameInputFeedbackEffectState.Stopped; }
    }

    /// <summary>Sets the state of a force feedback effect (Stopped, Running, Paused).</summary>
    public static void SetForceFeedbackEffectState(IntPtr effectPtr, int state)
    {
        if (effectPtr == IntPtr.Zero) return;
        try
        {
            var effect = (Native.IGameInputForceFeedbackEffect)Marshal.GetObjectForIUnknown(effectPtr);
            effect.SetState(state);
        }
        catch { /* ignore */ }
    }

    /// <summary>Gets the gain of a force feedback effect (0–1).</summary>
    public static float GetForceFeedbackEffectGain(IntPtr effectPtr)
    {
        if (effectPtr == IntPtr.Zero) return 0f;
        try
        {
            var effect = (Native.IGameInputForceFeedbackEffect)Marshal.GetObjectForIUnknown(effectPtr);
            return effect.GetGain();
        }
        catch { return 0f; }
    }

    /// <summary>Sets the gain of a force feedback effect (0–1).</summary>
    public static void SetForceFeedbackEffectGain(IntPtr effectPtr, float gain)
    {
        if (effectPtr == IntPtr.Zero) return;
        try
        {
            var effect = (Native.IGameInputForceFeedbackEffect)Marshal.GetObjectForIUnknown(effectPtr);
            effect.SetGain(Math.Clamp(gain, 0f, 1f));
        }
        catch { /* ignore */ }
    }

    /// <summary>Gets the motor index (0–7) for a force feedback effect.</summary>
    public static uint GetForceFeedbackEffectMotorIndex(IntPtr effectPtr)
    {
        if (effectPtr == IntPtr.Zero) return 0;
        try
        {
            var effect = (Native.IGameInputForceFeedbackEffect)Marshal.GetObjectForIUnknown(effectPtr);
            return effect.GetMotorIndex();
        }
        catch { return 0; }
    }

    /// <summary>Releases a force feedback effect pointer from CreateForceFeedbackEffectConstantRetain.</summary>
    public static void ReleaseForceFeedbackEffect(IntPtr effectPtr)
    {
        if (effectPtr != IntPtr.Zero)
            Marshal.Release(effectPtr);
    }

    /// <summary>Sets simple rumble on a device. Left/right in [0,1].</summary>
    public static void SetRumble(IntPtr devicePtr, float lowFrequency, float highFrequency, float leftTrigger = 0f, float rightTrigger = 0f)
    {
        if (devicePtr == IntPtr.Zero)
            return;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            var rumble = new GameInputRumbleParams
            {
                LowFrequency = Math.Clamp(lowFrequency, 0f, 1f),
                HighFrequency = Math.Clamp(highFrequency, 0f, 1f),
                LeftTrigger = Math.Clamp(leftTrigger, 0f, 1f),
                RightTrigger = Math.Clamp(rightTrigger, 0f, 1f)
            };
            device.SetRumbleState(rumble);
        }
        catch { /* ignore */ }
    }

    /// <summary>Releases a device pointer obtained from EnumerateDevices.</summary>
    public static void ReleaseDevice(IntPtr devicePtr)
    {
        if (devicePtr != IntPtr.Zero)
            Marshal.Release(devicePtr);
    }

    /// <summary>Gets the current timestamp in microseconds (from IGameInput).</summary>
    public static ulong GetCurrentTimestamp(IGameInput gameInput)
    {
        if (gameInput == null) return 0;
        try { return gameInput.GetCurrentTimestamp(); }
        catch { return 0; }
    }

    private static bool IsDeviceIdAllZero(byte[] deviceIdBytes)
    {
        for (int i = 0; i < Native.AppLocalDeviceId.Size && i < deviceIdBytes.Length; i++)
            if (deviceIdBytes[i] != 0) return false;
        return true;
    }

    /// <summary>Finds a device by its app-local ID. Caller must release the returned pointer or wrap in a device. Rejects all-zero device IDs. See docs/SECURITY.md.</summary>
    public static IntPtr FindDeviceFromId(IGameInput gameInput, byte[] deviceIdBytes)
    {
        if (gameInput == null || deviceIdBytes == null || deviceIdBytes.Length < Native.AppLocalDeviceId.Size)
            return IntPtr.Zero;
        if (IsDeviceIdAllZero(deviceIdBytes))
            return IntPtr.Zero;
        try
        {
            var id = Native.AppLocalDeviceId.Create();
            Array.Copy(deviceIdBytes, 0, id.Value!, 0, Native.AppLocalDeviceId.Size);
            int hr = gameInput.FindDeviceFromId(in id, out IntPtr device);
            if (hr != S_OK || device == IntPtr.Zero) return IntPtr.Zero;
            Marshal.AddRef(device);
            return device;
        }
        catch { return IntPtr.Zero; }
    }

    /// <summary>Finds a device by platform string. Caller must release the returned pointer or wrap in a device.</summary>
    /// <remarks>Platform string length is capped at 2048 characters to reduce native API abuse; longer strings return IntPtr.Zero. See docs/SECURITY.md.</remarks>
    public static IntPtr FindDeviceFromPlatformString(IGameInput gameInput, string platformString)
    {
        if (gameInput == null || string.IsNullOrEmpty(platformString))
            return IntPtr.Zero;
        if (platformString.Length > MaxPlatformStringLength)
            return IntPtr.Zero;
        try
        {
            int hr = gameInput.FindDeviceFromPlatformString(platformString, out IntPtr device);
            if (hr != S_OK || device == IntPtr.Zero) return IntPtr.Zero;
            Marshal.AddRef(device);
            return device;
        }
        catch { return IntPtr.Zero; }
    }

    /// <summary>Sets the input focus policy (Default, Background, Exclusive).</summary>
    public static void SetFocusPolicy(IGameInput gameInput, uint policy)
    {
        if (gameInput == null) return;
        try { gameInput.SetFocusPolicy(policy); }
        catch { /* ignore */ }
    }

    /// <summary>Gets device status flags (e.g. GameInputDeviceConnected).</summary>
    public static uint GetDeviceStatus(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero) return 0;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            return device.GetDeviceStatus();
        }
        catch { return 0; }
    }

    /// <summary>Gets haptic info (location count, etc.).</summary>
    public static bool GetHapticInfo(IntPtr devicePtr, out Native.GameInputHapticInfo info)
    {
        info = default;
        if (devicePtr == IntPtr.Zero) return false;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            return device.GetHapticInfo(out info) == S_OK;
        }
        catch { return false; }
    }

    /// <summary>Sets the master gain for a force feedback motor (0–1).</summary>
    public static void SetForceFeedbackMotorGain(IntPtr devicePtr, uint motorIndex, float gain)
    {
        if (devicePtr == IntPtr.Zero || motorIndex > 7) return;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            device.SetForceFeedbackMotorGain(motorIndex, Math.Clamp(gain, 0f, 1f));
        }
        catch { /* ignore */ }
    }

    /// <summary>Returns whether the force feedback motor is powered on.</summary>
    public static bool IsForceFeedbackMotorPoweredOn(IntPtr devicePtr, uint motorIndex)
    {
        if (devicePtr == IntPtr.Zero || motorIndex > 7) return false;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            return device.IsForceFeedbackMotorPoweredOn(motorIndex);
        }
        catch { return false; }
    }

    /// <summary>Gets the number of extra axes for an input kind. Returns 0 on failure.</summary>
    public static uint GetExtraAxisCount(IntPtr devicePtr, uint inputKind)
    {
        if (devicePtr == IntPtr.Zero) return 0;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            return device.GetExtraAxisCount(inputKind, out uint count) == S_OK ? count : 0;
        }
        catch { return 0; }
    }

    /// <summary>Gets the number of extra buttons for an input kind. Returns 0 on failure.</summary>
    public static uint GetExtraButtonCount(IntPtr devicePtr, uint inputKind)
    {
        if (devicePtr == IntPtr.Zero) return 0;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            return device.GetExtraButtonCount(inputKind, out uint count) == S_OK ? count : 0;
        }
        catch { return 0; }
    }

    /// <summary>Gets the extra axis indexes for an input kind. Returns array (may be empty).</summary>
    /// <remarks>Count from native is capped at 1024 to prevent DoS from buggy or malicious drivers. See docs/SECURITY.md.</remarks>
    public static uint[] GetExtraAxisIndexes(IntPtr devicePtr, uint inputKind)
    {
        if (devicePtr == IntPtr.Zero) return Array.Empty<uint>();
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            if (device.GetExtraAxisCount(inputKind, out uint count) != S_OK || count == 0)
                return Array.Empty<uint>();
            count = (uint)Math.Min(count, MaxExtraAxisOrButtonCount);
            var buffer = new uint[count];
            IntPtr ptr = Marshal.AllocHGlobal((int)(count * sizeof(uint)));
            try
            {
                if (device.GetExtraAxisIndexes(inputKind, count, ptr) != S_OK)
                    return Array.Empty<uint>();
                for (int i = 0; i < count; i++)
                    buffer[i] = (uint)Marshal.ReadInt32(ptr, i * sizeof(uint));
                return buffer;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        catch { return Array.Empty<uint>(); }
    }

    /// <summary>Gets the extra button indexes for an input kind. Returns array (may be empty).</summary>
    /// <remarks>Count from native is capped at 1024 to prevent DoS from buggy or malicious drivers. See docs/SECURITY.md.</remarks>
    public static uint[] GetExtraButtonIndexes(IntPtr devicePtr, uint inputKind)
    {
        if (devicePtr == IntPtr.Zero) return Array.Empty<uint>();
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            if (device.GetExtraButtonCount(inputKind, out uint count) != S_OK || count == 0)
                return Array.Empty<uint>();
            count = (uint)Math.Min(count, MaxExtraAxisOrButtonCount);
            var buffer = new uint[count];
            IntPtr ptr = Marshal.AllocHGlobal((int)(count * sizeof(uint)));
            try
            {
                if (device.GetExtraButtonIndexes(inputKind, count, ptr) != S_OK)
                    return Array.Empty<uint>();
                for (int i = 0; i < count; i++)
                    buffer[i] = (uint)Marshal.ReadInt32(ptr, i * sizeof(uint));
                return buffer;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        catch { return Array.Empty<uint>(); }
    }

    /// <summary>DirectInput escape: send a command to the device. Returns (success, bytesWritten).</summary>
    /// <remarks>Buffer sizes are capped at 64 KB per buffer; larger buffers return (false, 0). See docs/SECURITY.md.</remarks>
    public static (bool Success, uint BytesWritten) DirectInputEscape(IntPtr devicePtr, uint command, byte[]? bufferIn, byte[]? bufferOut)
    {
        if (devicePtr == IntPtr.Zero) return (false, 0);
        if (bufferIn != null && bufferIn.Length > MaxDirectInputEscapeBufferSize) return (false, 0);
        if (bufferOut != null && bufferOut.Length > MaxDirectInputEscapeBufferSize) return (false, 0);
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            uint inSize = (uint)(bufferIn?.Length ?? 0);
            uint outSize = (uint)(bufferOut?.Length ?? 0);
            IntPtr pIn = IntPtr.Zero, pOut = IntPtr.Zero;
            if (bufferIn != null && bufferIn.Length > 0)
            {
                pIn = Marshal.AllocHGlobal(bufferIn.Length);
                Marshal.Copy(bufferIn, 0, pIn, bufferIn.Length);
            }
            if (bufferOut != null && bufferOut.Length > 0)
                pOut = Marshal.AllocHGlobal(bufferOut.Length);
            try
            {
                int hr = device.DirectInputEscape(command, pIn, inSize, pOut, outSize, out uint written);
                if (pOut != IntPtr.Zero && bufferOut != null && written > 0)
                    Marshal.Copy(pOut, bufferOut, 0, (int)Math.Min(written, bufferOut.Length));
                return (hr == S_OK, written);
            }
            finally
            {
                if (pIn != IntPtr.Zero) Marshal.FreeHGlobal(pIn);
                if (pOut != IntPtr.Zero) Marshal.FreeHGlobal(pOut);
            }
        }
        catch { return (false, 0); }
    }

    /// <summary>Creates an input mapper for the device. Prefer wrapping in <see cref="Abstractions.InputMapper"/> and disposing when done.</summary>
    public static IntPtr CreateInputMapper(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero) return IntPtr.Zero;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            if (device.CreateInputMapper(out IntPtr mapper) != S_OK || mapper == IntPtr.Zero) return IntPtr.Zero;
            Marshal.AddRef(mapper);
            return mapper;
        }
        catch { return IntPtr.Zero; }
    }

    // IGameInputMapper vtable slots (3–9); slot 0–2 are IUnknown.
    private const int MapperSlotGamepadAxis = 3;
    private const int MapperSlotGamepadButton = 4;
    private const int MapperSlotFlightStickAxis = 5;
    private const int MapperSlotFlightStickButton = 6;
    private const int MapperSlotRacingWheelAxis = 7;
    private const int MapperSlotRacingWheelButton = 8;
    private const int MapperSlotArcadeStickButton = 9;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool GetAxisMappingDelegate(IntPtr thisPtr, int element, out Native.GameInputAxisMapping mapping);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool GetButtonMappingDelegate(IntPtr thisPtr, int element, out Native.GameInputButtonMapping mapping);

    private static bool TryGetMapperAxis(IntPtr mapperPtr, int slot, int element, out Native.GameInputAxisMapping mapping)
    {
        mapping = default;
        if (mapperPtr == IntPtr.Zero) return false;
        try
        {
            IntPtr vtable = Marshal.ReadIntPtr(mapperPtr);
            IntPtr fn = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
            if (fn == IntPtr.Zero) return false;
            var d = Marshal.GetDelegateForFunctionPointer<GetAxisMappingDelegate>(fn);
            return d(mapperPtr, element, out mapping);
        }
        catch { return false; }
    }

    private static bool TryGetMapperButton(IntPtr mapperPtr, int slot, int element, out Native.GameInputButtonMapping mapping)
    {
        mapping = default;
        if (mapperPtr == IntPtr.Zero) return false;
        try
        {
            IntPtr vtable = Marshal.ReadIntPtr(mapperPtr);
            IntPtr fn = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
            if (fn == IntPtr.Zero) return false;
            var d = Marshal.GetDelegateForFunctionPointer<GetButtonMappingDelegate>(fn);
            return d(mapperPtr, element, out mapping);
        }
        catch { return false; }
    }

    /// <summary>Gets gamepad axis mapping. Used by <see cref="Abstractions.InputMapper"/>.</summary>
    public static bool TryGetGamepadAxisMappingInfo(IntPtr mapperPtr, int axisElement, out Native.GameInputAxisMapping mapping) =>
        TryGetMapperAxis(mapperPtr, MapperSlotGamepadAxis, axisElement, out mapping);

    /// <summary>Gets gamepad button mapping.</summary>
    public static bool TryGetGamepadButtonMappingInfo(IntPtr mapperPtr, int buttonElement, out Native.GameInputButtonMapping mapping) =>
        TryGetMapperButton(mapperPtr, MapperSlotGamepadButton, buttonElement, out mapping);

    /// <summary>Gets flight stick axis mapping.</summary>
    public static bool TryGetFlightStickAxisMappingInfo(IntPtr mapperPtr, int axisElement, out Native.GameInputAxisMapping mapping) =>
        TryGetMapperAxis(mapperPtr, MapperSlotFlightStickAxis, axisElement, out mapping);

    /// <summary>Gets flight stick button mapping.</summary>
    public static bool TryGetFlightStickButtonMappingInfo(IntPtr mapperPtr, int buttonElement, out Native.GameInputButtonMapping mapping) =>
        TryGetMapperButton(mapperPtr, MapperSlotFlightStickButton, buttonElement, out mapping);

    /// <summary>Gets racing wheel axis mapping.</summary>
    public static bool TryGetRacingWheelAxisMappingInfo(IntPtr mapperPtr, int axisElement, out Native.GameInputAxisMapping mapping) =>
        TryGetMapperAxis(mapperPtr, MapperSlotRacingWheelAxis, axisElement, out mapping);

    /// <summary>Gets racing wheel button mapping.</summary>
    public static bool TryGetRacingWheelButtonMappingInfo(IntPtr mapperPtr, int buttonElement, out Native.GameInputButtonMapping mapping) =>
        TryGetMapperButton(mapperPtr, MapperSlotRacingWheelButton, buttonElement, out mapping);

    /// <summary>Gets arcade stick button mapping.</summary>
    public static bool TryGetArcadeStickButtonMappingInfo(IntPtr mapperPtr, int buttonElement, out Native.GameInputButtonMapping mapping) =>
        TryGetMapperButton(mapperPtr, MapperSlotArcadeStickButton, buttonElement, out mapping);

    /// <summary>Creates a raw device report. Caller must Marshal.Release the returned pointer when done.</summary>
    public static IntPtr CreateRawDeviceReport(IntPtr devicePtr, uint reportId, int reportKind)
    {
        if (devicePtr == IntPtr.Zero) return IntPtr.Zero;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            if (device.CreateRawDeviceReport(reportId, reportKind, out IntPtr report) != S_OK || report == IntPtr.Zero) return IntPtr.Zero;
            Marshal.AddRef(report);
            return report;
        }
        catch { return IntPtr.Zero; }
    }

    /// <summary>Sends a raw device output report.</summary>
    public static bool SendRawDeviceOutput(IntPtr devicePtr, IntPtr reportPtr)
    {
        if (devicePtr == IntPtr.Zero || reportPtr == IntPtr.Zero) return false;
        try
        {
            var device = (IGameInputDevice)Marshal.GetObjectForIUnknown(devicePtr);
            return device.SendRawDeviceOutput(reportPtr) == S_OK;
        }
        catch { return false; }
    }

    /// <summary>Creates an aggregate device. Returns the new device ID; caller can FindDeviceFromId to get the device.</summary>
    public static bool CreateAggregateDevice(IGameInput gameInput, uint inputKind, out byte[] deviceIdOut)
    {
        deviceIdOut = Array.Empty<byte>();
        if (gameInput == null) return false;
        try
        {
            int hr = gameInput.CreateAggregateDevice(inputKind, out Native.AppLocalDeviceId id);
            if (hr != S_OK || id.Value == null) return false;
            deviceIdOut = new byte[Native.AppLocalDeviceId.Size];
            Array.Copy(id.Value, deviceIdOut, Native.AppLocalDeviceId.Size);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Disables an aggregate device by ID. Rejects all-zero device IDs. See docs/SECURITY.md.</summary>
    public static bool DisableAggregateDevice(IGameInput gameInput, byte[] deviceIdBytes)
    {
        if (gameInput == null || deviceIdBytes == null || deviceIdBytes.Length < Native.AppLocalDeviceId.Size)
            return false;
        if (IsDeviceIdAllZero(deviceIdBytes))
            return false;
        try
        {
            var id = Native.AppLocalDeviceId.Create();
            Array.Copy(deviceIdBytes, 0, id.Value!, 0, Native.AppLocalDeviceId.Size);
            return gameInput.DisableAggregateDevice(in id) == S_OK;
        }
        catch { return false; }
    }

    /// <summary>Gets the next reading after a reference reading. Caller must release the returned reading.</summary>
    public static IntPtr GetNextReading(IGameInput gameInput, IntPtr referenceReading, uint inputKind, IntPtr devicePtr)
    {
        if (gameInput == null) return IntPtr.Zero;
        try
        {
            int hr = gameInput.GetNextReading(referenceReading, inputKind, devicePtr, out IntPtr reading);
            if (hr != S_OK || reading == IntPtr.Zero) return IntPtr.Zero;
            Marshal.AddRef(reading);
            return reading;
        }
        catch { return IntPtr.Zero; }
    }

    /// <summary>Gets the previous reading before a reference reading. Caller must release the returned reading.</summary>
    public static IntPtr GetPreviousReading(IGameInput gameInput, IntPtr referenceReading, uint inputKind, IntPtr devicePtr)
    {
        if (gameInput == null) return IntPtr.Zero;
        try
        {
            int hr = gameInput.GetPreviousReading(referenceReading, inputKind, devicePtr, out IntPtr reading);
            if (hr != S_OK || reading == IntPtr.Zero) return IntPtr.Zero;
            Marshal.AddRef(reading);
            return reading;
        }
        catch { return IntPtr.Zero; }
    }

    /// <summary>Gets the timestamp of a reading (microseconds).</summary>
    public static ulong GetReadingTimestamp(IntPtr readingPtr)
    {
        if (readingPtr == IntPtr.Zero) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetTimestamp();
        }
        catch { return 0; }
    }

    /// <summary>Gets the input kind of a reading.</summary>
    public static uint GetReadingInputKind(IntPtr readingPtr)
    {
        if (readingPtr == IntPtr.Zero) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetInputKind();
        }
        catch { return 0; }
    }

    /// <summary>Gets the device pointer from a reading. Caller must Marshal.Release the returned pointer when done.</summary>
    public static IntPtr GetDeviceFromReading(IntPtr readingPtr)
    {
        if (readingPtr == IntPtr.Zero) return IntPtr.Zero;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            reading.GetDevice(out IntPtr device);
            if (device != IntPtr.Zero) Marshal.AddRef(device);
            return device;
        }
        catch { return IntPtr.Zero; }
    }

    /// <summary>Gets the number of controller axes in the reading.</summary>
    public static uint GetControllerAxisCount(IntPtr readingPtr)
    {
        if (readingPtr == IntPtr.Zero) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetControllerAxisCount();
        }
        catch { return 0; }
    }

    /// <summary>Gets controller axis state (floats). Returns number of values written.</summary>
    public static uint GetControllerAxisState(IntPtr readingPtr, float[] buffer)
    {
        if (readingPtr == IntPtr.Zero || buffer == null || buffer.Length == 0) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            uint count = reading.GetControllerAxisCount();
            if (count == 0) return 0;
            int toCopy = (int)Math.Min(count, (uint)buffer.Length);
            IntPtr ptr = Marshal.AllocHGlobal(toCopy * sizeof(float));
            try
            {
                uint written = reading.GetControllerAxisState((uint)toCopy, ptr);
                for (int i = 0; i < written && i < buffer.Length; i++)
                    buffer[i] = Marshal.PtrToStructure<float>(IntPtr.Add(ptr, i * sizeof(float)));
                return written;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        catch { return 0; }
    }

    /// <summary>Gets the number of controller buttons in the reading.</summary>
    public static uint GetControllerButtonCount(IntPtr readingPtr)
    {
        if (readingPtr == IntPtr.Zero) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetControllerButtonCount();
        }
        catch { return 0; }
    }

    /// <summary>Gets controller button state (uints). Returns number of values written.</summary>
    public static uint GetControllerButtonState(IntPtr readingPtr, uint[] buffer)
    {
        if (readingPtr == IntPtr.Zero || buffer == null || buffer.Length == 0) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            uint count = reading.GetControllerButtonCount();
            if (count == 0) return 0;
            int toCopy = (int)Math.Min(count, (uint)buffer.Length);
            IntPtr ptr = Marshal.AllocHGlobal(toCopy * sizeof(uint));
            try
            {
                uint written = reading.GetControllerButtonState((uint)toCopy, ptr);
                for (int i = 0; i < written && i < buffer.Length; i++)
                    buffer[i] = (uint)Marshal.ReadInt32(ptr, i * sizeof(uint));
                return written;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        catch { return 0; }
    }

    /// <summary>Gets the number of controller switches in the reading.</summary>
    public static uint GetControllerSwitchCount(IntPtr readingPtr)
    {
        if (readingPtr == IntPtr.Zero) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetControllerSwitchCount();
        }
        catch { return 0; }
    }

    /// <summary>Gets controller switch state (positions as int). Returns number of values written.</summary>
    public static uint GetControllerSwitchState(IntPtr readingPtr, int[] buffer)
    {
        if (readingPtr == IntPtr.Zero || buffer == null || buffer.Length == 0) return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            uint count = reading.GetControllerSwitchCount();
            if (count == 0) return 0;
            int toCopy = (int)Math.Min(count, (uint)buffer.Length);
            IntPtr ptr = Marshal.AllocHGlobal(toCopy * sizeof(int));
            try
            {
                uint written = reading.GetControllerSwitchState((uint)toCopy, ptr);
                for (int i = 0; i < written && i < buffer.Length; i++)
                    buffer[i] = Marshal.ReadInt32(ptr, i * sizeof(int));
                return written;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        catch { return 0; }
    }

    /// <summary>Gets the raw report pointer for the reading. Do not release; valid only for the lifetime of the reading.</summary>
    public static bool GetRawReportFromReading(IntPtr readingPtr, out IntPtr reportPtr)
    {
        reportPtr = IntPtr.Zero;
        if (readingPtr == IntPtr.Zero) return false;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetRawReport(out reportPtr);
        }
        catch { return false; }
    }

    /// <summary>Gets the current reading for a device and input kind. Caller must call ReleaseReading when done.</summary>
    public static IntPtr GetCurrentReading(IGameInput gameInput, IntPtr devicePtr, uint inputKind)
    {
        if (gameInput == null || devicePtr == IntPtr.Zero)
            return IntPtr.Zero;
        try
        {
            int hr = gameInput.GetCurrentReading(inputKind, devicePtr, out IntPtr reading);
            if (hr != S_OK || reading == IntPtr.Zero)
                return IntPtr.Zero;
            Marshal.AddRef(reading);
            return reading;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>Releases a reading pointer from GetCurrentReading.</summary>
    public static void ReleaseReading(IntPtr readingPtr)
    {
        if (readingPtr != IntPtr.Zero)
            Marshal.Release(readingPtr);
    }

    /// <summary>Gets gamepad state from a reading. Returns false if reading is null or not gamepad.</summary>
    public static bool GetGamepadStateFromReading(IntPtr readingPtr, out Native.GameInputGamepadState state)
    {
        state = default;
        if (readingPtr == IntPtr.Zero)
            return false;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetGamepadState(out state);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets mouse state from a reading. Returns false if reading is null or not mouse.</summary>
    public static bool GetMouseStateFromReading(IntPtr readingPtr, out Native.GameInputMouseState state)
    {
        state = default;
        if (readingPtr == IntPtr.Zero)
            return false;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetMouseState(out state);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets key count from a reading. Returns 0 if not keyboard.</summary>
    public static uint GetKeyCountFromReading(IntPtr readingPtr)
    {
        if (readingPtr == IntPtr.Zero)
            return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetKeyCount();
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Gets key state from a reading. buffer must be at least keyCount * sizeof(GameInputKeyState).</summary>
    public static uint GetKeyStateFromReading(IntPtr readingPtr, Native.GameInputKeyState[] buffer)
    {
        if (readingPtr == IntPtr.Zero || buffer == null || buffer.Length == 0)
            return 0;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            uint count = reading.GetKeyCount();
            if (count == 0)
                return 0;
            int bytes = buffer.Length * Marshal.SizeOf<Native.GameInputKeyState>();
            IntPtr ptr = Marshal.AllocHGlobal(bytes);
            try
            {
                uint written = reading.GetKeyState((uint)buffer.Length, ptr);
                for (int i = 0; i < written && i < buffer.Length; i++)
                    buffer[i] = Marshal.PtrToStructure<Native.GameInputKeyState>(IntPtr.Add(ptr, i * Marshal.SizeOf<Native.GameInputKeyState>()));
                return written;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Gets sensors (motion) state from a reading. Returns false if reading is null or not sensors.</summary>
    public static bool GetSensorsStateFromReading(IntPtr readingPtr, out Native.GameInputSensorsState state)
    {
        state = default;
        if (readingPtr == IntPtr.Zero) return false;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetSensorsState(out state);
        }
        catch { return false; }
    }

    /// <summary>Gets arcade stick state from a reading.</summary>
    public static bool GetArcadeStickStateFromReading(IntPtr readingPtr, out Native.GameInputArcadeStickState state)
    {
        state = default;
        if (readingPtr == IntPtr.Zero) return false;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetArcadeStickState(out state);
        }
        catch { return false; }
    }

    /// <summary>Gets flight stick state from a reading.</summary>
    public static bool GetFlightStickStateFromReading(IntPtr readingPtr, out Native.GameInputFlightStickState state)
    {
        state = default;
        if (readingPtr == IntPtr.Zero) return false;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetFlightStickState(out state);
        }
        catch { return false; }
    }

    /// <summary>Gets racing wheel state from a reading.</summary>
    public static bool GetRacingWheelStateFromReading(IntPtr readingPtr, out Native.GameInputRacingWheelState state)
    {
        state = default;
        if (readingPtr == IntPtr.Zero) return false;
        try
        {
            var reading = (Native.IGameInputReading)Marshal.GetObjectForIUnknown(readingPtr);
            return reading.GetRacingWheelState(out state);
        }
        catch { return false; }
    }
}
