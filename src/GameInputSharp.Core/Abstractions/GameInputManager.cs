// GameInputSharp.Abstractions — high-level public APIs.
// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.
// Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GameInputSharp.Core;
using GameInputSharp.Devices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameInputSharp.Abstractions;

/// <summary>
/// Central manager for GameInput: device discovery, polling, and lifecycle.
/// </summary>
/// <remarks>
/// On Windows with the GameInput runtime installed, <see cref="GetDevices"/> returns gamepads, keyboards, and mice.
/// If the runtime is missing or init fails, <see cref="GetDevices"/> returns an empty list without throwing.
/// Subscribe to <see cref="DeviceCallback"/> and call <see cref="DispatchCallbacks"/> from your game loop for connect/disconnect events.
/// Dispose the manager when done to release native references.
/// <para><strong>Thread safety:</strong> This manager is not thread-safe. Use it from a single thread (e.g. game loop) or synchronize access externally. Call <see cref="DispatchCallbacks"/> only from one thread.</para>
/// <para><strong>Disposal:</strong> On <see cref="Dispose"/>, the wrapper unregisters all callbacks and then frees the context handle. Do not call <see cref="Dispose"/> or <see cref="UnregisterCallback(ulong)"/> from inside a <see cref="DeviceCallback"/> or <see cref="ReadingCallback"/> handler. Callers own device wrappers returned from <see cref="GetDevices"/> and must dispose them when done. See docs/SECURITY.md.</para>
/// </remarks>
public sealed class GameInputManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly bool _loadOnlyFromSystem32;
    private bool _disposed;
    private Core.Native.IGameInput? _gameInput;
    private readonly ConcurrentQueue<(ulong, uint, uint, string)> _deviceCallbackQueue = new();
    private GCHandle _deviceCallbackHandle;
    private ulong _deviceCallbackToken;
    private bool _deviceCallbackRegistered;
    private readonly ConcurrentQueue<(IntPtr, bool)> _readingCallbackQueue = new();
    private GCHandle _readingCallbackHandle;
    private readonly List<ulong> _readingCallbackTokens = new();
    private bool _readingCallbackHandleAllocated;
    private bool _inCallback;

    /// <summary>Raised when a device connects or disconnects. Call <see cref="DispatchCallbacks"/> to pump.</summary>
    public event EventHandler<DeviceCallbackEventArgs>? DeviceCallback;

    /// <summary>Raised when a new input reading is available (after <see cref="RegisterReadingCallback(IInputDevice?, uint)"/> and <see cref="DispatchCallbacks"/>). Dispose <see cref="ReadingCallbackEventArgs.Reading"/> when done.</summary>
    public event EventHandler<ReadingCallbackEventArgs>? ReadingCallback;

    /// <summary>Creates a manager with optional logging.</summary>
    /// <param name="logger">Logger for diagnostics; uses NullLogger if not provided.</param>
    public GameInputManager(ILogger? logger = null)
        : this(logger, loadOnlyFromSystem32: false)
    {
    }

    /// <summary>Creates a manager with optional logging and DLL load policy.</summary>
    /// <param name="logger">Logger for diagnostics; uses NullLogger if not provided.</param>
    /// <param name="loadOnlyFromSystem32">If true, load GameInput DLL only from System32 (no app directory). Use for maximum security against DLL hijacking; see docs/SECURITY.md.</param>
    public GameInputManager(ILogger? logger, bool loadOnlyFromSystem32)
    {
        _logger = logger ?? NullLogger.Instance;
        _loadOnlyFromSystem32 = loadOnlyFromSystem32;
    }

    /// <summary>
    /// Enumerates currently connected input devices (gamepads, keyboards, mice).
    /// </summary>
    /// <returns>Collection of discovered devices. Empty if GameInput runtime is unavailable.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
    public IReadOnlyList<IInputDevice> GetDevices()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var gameInput = GetOrCreateGameInput();
        if (gameInput == null)
        {
            _logger.LogDebug("GameInput not available (DLL missing or init failed).");
            return Array.Empty<IInputDevice>();
        }

        var devicePtrs = GameInputInterop.EnumerateDevices(gameInput);
        var list = new List<IInputDevice>(devicePtrs.Count);
        foreach (var ptr in devicePtrs)
        {
            try
            {
                var device = DeviceFactory.CreateFromNative(ptr);
                if (device != null)
                    list.Add(device);
                else
                    GameInputInterop.ReleaseDevice(ptr);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to wrap device {Ptr}.", ptr);
                GameInputInterop.ReleaseDevice(ptr);
            }
        }
        return list;
    }

    /// <summary>Tries to get a device by its stable ID (e.g. from <see cref="DeviceCallbackEventArgs.DeviceId"/>). Enumerates devices and returns the first match; use when you need a wrapper for a known ID without holding a reference.</summary>
    /// <param name="deviceId">Stable device ID string (same as <see cref="IInputDevice.DeviceId"/>).</param>
    /// <param name="device">Receives the device wrapper when found; caller owns and should dispose when done.</param>
    /// <returns>True if a device with the given ID was found.</returns>
    public bool TryGetDeviceByDeviceId(string? deviceId, out IInputDevice? device)
    {
        device = null;
        if (string.IsNullOrEmpty(deviceId)) return false;
        var devices = GetDevices();
        foreach (var d in devices)
        {
            if (d.DeviceId == deviceId)
            {
                device = d;
                return true;
            }
        }
        return false;
    }

    /// <summary>Diagnostic: paths and file existence for GameInput DLLs, and process bitness. Call when DLL fails to load.</summary>
    public (string GameInputDllPath, string GameInputRedistPath, bool GameInputDllExists, bool GameInputRedistExists, bool Is64BitProcess) GetLoadPaths()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return GameInputInterop.GetLoadPaths();
    }

    /// <summary>When the DLL file exists but load failed: returns the Win32 error and a short message (e.g. 126 = missing dependency).</summary>
    public (int Win32Error, string Message) GetLastLoadError()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return GameInputInterop.GetLastLoadError();
    }

    /// <summary>After init fails: returns the Win32 error and/or exception from the main load path (TryLoadByPath). Call this first before other load diagnostics so the value from the initial load attempt is still present.</summary>
    public (int Win32Error, string? ExceptionMessage) GetMainPathLoadFailure()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return GameInputInterop.GetMainPathLoadFailure();
    }

    /// <summary>Diagnostic: reports why GameInput might have failed to load. Call when init fails to see if the DLL is missing or GameInputCreate returned an error code.</summary>
    /// <returns>DllLoaded true if the native DLL was found and loaded; InitHResult is 0 on success, or the HRESULT from GameInputCreate (e.g. 0x80070005 = E_ACCESSDENIED), or -1 if the DLL has no GameInputCreate export.</returns>
    public (bool DllLoaded, int InitHResult) GetLoadDiagnostics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return GameInputInterop.GetLoadDiagnostics();
    }

    /// <summary>Diagnostic: reports whether GameInput init succeeded and how many raw devices the API returned. Use when GetDevices() is empty to see if the DLL/init works but no devices are exposed (e.g. some controllers only appear under XInput).</summary>
    /// <returns>InitSucceeded true if the native runtime loaded and GameInputCreate succeeded; RawDeviceCount is the number of devices enumerated (before wrapping).</returns>
    public (bool InitSucceeded, int RawDeviceCount) GetDiagnostics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi == null) return (false, 0);
        var devicePtrs = GameInputInterop.EnumerateDevices(gi);
        int count = devicePtrs.Count;
        foreach (IntPtr ptr in devicePtrs)
            GameInputInterop.ReleaseDevice(ptr);
        return (true, count);
    }

    /// <summary>Gets the current timestamp in microseconds (for latency/timing).</summary>
    public ulong GetCurrentTimestamp()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        return gi != null ? GameInputInterop.GetCurrentTimestamp(gi) : 0;
    }

    /// <summary>Finds a device by its app-local ID (e.g. from a previous enumeration). Caller owns the returned device; dispose when done. All-zero device IDs are rejected. See docs/SECURITY.md.</summary>
    /// <param name="deviceIdBytes">32-byte device ID (e.g. from <see cref="IInputDevice.DeviceId"/> decoded from hex, or from <see cref="CreateAggregateDevice"/>).</param>
    public IInputDevice? FindDeviceFromId(byte[] deviceIdBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi == null || deviceIdBytes == null || deviceIdBytes.Length < 32) return null;
        IntPtr ptr = GameInputInterop.FindDeviceFromId(gi, deviceIdBytes);
        if (ptr == IntPtr.Zero) return null;
        try
        {
            var device = DeviceFactory.CreateFromNative(ptr);
            if (device == null) GameInputInterop.ReleaseDevice(ptr);
            return device;
        }
        catch
        {
            GameInputInterop.ReleaseDevice(ptr);
            return null;
        }
    }

    /// <summary>Finds a device by platform-specific string. Caller owns the returned device; dispose when done.</summary>
    public IInputDevice? FindDeviceFromPlatformString(string platformString)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi == null || string.IsNullOrEmpty(platformString)) return null;
        IntPtr ptr = GameInputInterop.FindDeviceFromPlatformString(gi, platformString);
        if (ptr == IntPtr.Zero) return null;
        try
        {
            var device = DeviceFactory.CreateFromNative(ptr);
            if (device == null) GameInputInterop.ReleaseDevice(ptr);
            return device;
        }
        catch
        {
            GameInputInterop.ReleaseDevice(ptr);
            return null;
        }
    }

    /// <summary>Sets the input focus policy (Default = 0, Background = 1, Exclusive = 2).</summary>
    public void SetFocusPolicy(uint policy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi != null) GameInputInterop.SetFocusPolicy(gi, policy);
    }

    /// <summary>Gets the current gamepad state (buttons, triggers, thumbsticks).</summary>
    /// <param name="gamepad">The gamepad device to poll.</param>
    /// <returns>Current state, or null if unavailable or device is not a gamepad.</returns>
    public GamepadState? GetCurrentGamepadState(GamepadDevice? gamepad)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (gamepad == null)
            return null;
        var gi = GetOrCreateGameInput();
        if (gi == null)
            return null;
        IntPtr reading = GameInputInterop.GetCurrentReading(gi, gamepad.DevicePtr, GameInputNative.GameInputKindGamepad);
        if (reading == IntPtr.Zero)
            return null;
        try
        {
            if (!GameInputInterop.GetGamepadStateFromReading(reading, out var native))
            {
                _logger.LogDebug("GetGamepadStateFromReading failed for gamepad {DeviceId} despite valid reading.", gamepad.DeviceId);
                return null;
            }
            return new GamepadState
            {
                Buttons = native.Buttons,
                LeftTrigger = native.LeftTrigger,
                RightTrigger = native.RightTrigger,
                LeftThumbstickX = native.LeftThumbstickX,
                LeftThumbstickY = native.LeftThumbstickY,
                RightThumbstickX = native.RightThumbstickX,
                RightThumbstickY = native.RightThumbstickY
            };
        }
        finally
        {
            GameInputInterop.ReleaseReading(reading);
        }
    }

    /// <summary>Gets the current mouse state (buttons, position, wheel).</summary>
    /// <param name="mouse">The mouse device to poll.</param>
    /// <returns>Current state, or null if unavailable.</returns>
    public MouseState? GetCurrentMouseState(MouseDevice? mouse)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (mouse == null)
            return null;
        var gi = GetOrCreateGameInput();
        if (gi == null)
            return null;
        IntPtr reading = GameInputInterop.GetCurrentReading(gi, mouse.DevicePtr, GameInputNative.GameInputKindMouse);
        if (reading == IntPtr.Zero)
            return null;
        try
        {
            if (!GameInputInterop.GetMouseStateFromReading(reading, out var native))
                return null;
            return new MouseState
            {
                Buttons = native.Buttons,
                PositionX = native.PositionX,
                PositionY = native.PositionY,
                AbsolutePositionX = native.AbsolutePositionX,
                AbsolutePositionY = native.AbsolutePositionY,
                WheelX = native.WheelX,
                WheelY = native.WheelY
            };
        }
        finally
        {
            GameInputInterop.ReleaseReading(reading);
        }
    }

    /// <summary>Gets the current keyboard key states.</summary>
    /// <param name="keyboard">The keyboard device to poll.</param>
    /// <param name="maxKeys">Maximum number of key states to return (default 256). Capped at 1024. See docs/SECURITY.md.</param>
    /// <returns>Array of key states (may be empty if not a keyboard or no keys pressed).</returns>
    public KeyState[] GetCurrentKeyboardState(KeyboardDevice? keyboard, int maxKeys = 256)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (keyboard == null || maxKeys <= 0)
            return Array.Empty<KeyState>();
        maxKeys = Math.Min(maxKeys, 1024);  // Security: cap to reduce DoS (docs/SECURITY.md)
        var gi = GetOrCreateGameInput();
        if (gi == null)
            return Array.Empty<KeyState>();
        IntPtr reading = GameInputInterop.GetCurrentReading(gi, keyboard.DevicePtr, GameInputNative.GameInputKindKeyboard);
        if (reading == IntPtr.Zero)
            return Array.Empty<KeyState>();
        try
        {
            uint keyCount = GameInputInterop.GetKeyCountFromReading(reading);
            if (keyCount == 0)
                return Array.Empty<KeyState>();
            int toRead = (int)Math.Min(keyCount, (uint)maxKeys);
            var nativeKeys = new Core.Native.GameInputKeyState[toRead];
            uint written = GameInputInterop.GetKeyStateFromReading(reading, nativeKeys);
            var result = new KeyState[written];
            for (int i = 0; i < written; i++)
            {
                result[i] = new KeyState
                {
                    ScanCode = nativeKeys[i].ScanCode,
                    CodePoint = nativeKeys[i].CodePoint,
                    VirtualKey = nativeKeys[i].VirtualKey,
                    IsDeadKey = nativeKeys[i].IsDeadKey
                };
            }
            return result;
        }
        finally
        {
            GameInputInterop.ReleaseReading(reading);
        }
    }

    /// <summary>Gets the current sensors (motion) state for a device that supports sensors (e.g. gamepad with gyro).</summary>
    public SensorsState? GetCurrentSensorsState(GamepadDevice? device)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (device == null) return null;
        var gi = GetOrCreateGameInput();
        if (gi == null) return null;
        IntPtr reading = GameInputInterop.GetCurrentReading(gi, device.DevicePtr, GameInputNative.GameInputKindSensors);
        if (reading == IntPtr.Zero) return null;
        try
        {
            if (!GameInputInterop.GetSensorsStateFromReading(reading, out var native))
                return null;
            return new SensorsState
            {
                AccelerationInGX = native.AccelerationInGX, AccelerationInGY = native.AccelerationInGY, AccelerationInGZ = native.AccelerationInGZ,
                AngularVelocityInRadPerSecX = native.AngularVelocityInRadPerSecX, AngularVelocityInRadPerSecY = native.AngularVelocityInRadPerSecY, AngularVelocityInRadPerSecZ = native.AngularVelocityInRadPerSecZ,
                HeadingInDegreesFromMagneticNorth = native.HeadingInDegreesFromMagneticNorth, HeadingAccuracy = native.HeadingAccuracy,
                OrientationW = native.OrientationW, OrientationX = native.OrientationX, OrientationY = native.OrientationY, OrientationZ = native.OrientationZ
            };
        }
        finally { GameInputInterop.ReleaseReading(reading); }
    }

    /// <summary>Gets the current arcade stick state for a device that supports it.</summary>
    public ArcadeStickState? GetCurrentArcadeStickState(GamepadDevice? device)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (device == null) return null;
        var gi = GetOrCreateGameInput();
        if (gi == null) return null;
        IntPtr reading = GameInputInterop.GetCurrentReading(gi, device.DevicePtr, GameInputNative.GameInputKindArcadeStick);
        if (reading == IntPtr.Zero) return null;
        try
        {
            if (!GameInputInterop.GetArcadeStickStateFromReading(reading, out var native))
                return null;
            return new ArcadeStickState { Buttons = native.Buttons };
        }
        finally { GameInputInterop.ReleaseReading(reading); }
    }

    /// <summary>Gets the current flight stick state for a device that supports it.</summary>
    public FlightStickState? GetCurrentFlightStickState(GamepadDevice? device)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (device == null) return null;
        var gi = GetOrCreateGameInput();
        if (gi == null) return null;
        IntPtr reading = GameInputInterop.GetCurrentReading(gi, device.DevicePtr, GameInputNative.GameInputKindFlightStick);
        if (reading == IntPtr.Zero) return null;
        try
        {
            if (!GameInputInterop.GetFlightStickStateFromReading(reading, out var native))
                return null;
            return new FlightStickState
            {
                Buttons = native.Buttons, HatSwitch = native.HatSwitch,
                Roll = native.Roll, Pitch = native.Pitch, Yaw = native.Yaw, Throttle = native.Throttle
            };
        }
        finally { GameInputInterop.ReleaseReading(reading); }
    }

    /// <summary>Gets the current racing wheel state for a device that supports it.</summary>
    public RacingWheelState? GetCurrentRacingWheelState(GamepadDevice? device)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (device == null) return null;
        var gi = GetOrCreateGameInput();
        if (gi == null) return null;
        IntPtr reading = GameInputInterop.GetCurrentReading(gi, device.DevicePtr, GameInputNative.GameInputKindRacingWheel);
        if (reading == IntPtr.Zero) return null;
        try
        {
            if (!GameInputInterop.GetRacingWheelStateFromReading(reading, out var native))
                return null;
            return new RacingWheelState
            {
                Buttons = native.Buttons, PatternShifterGear = native.PatternShifterGear,
                Wheel = native.Wheel, Throttle = native.Throttle, Brake = native.Brake, Clutch = native.Clutch, Handbrake = native.Handbrake
            };
        }
        finally { GameInputInterop.ReleaseReading(reading); }
    }

    /// <summary>Creates an aggregate device for the given input kind. Returns the 32-byte device ID; use <see cref="FindDeviceFromId"/> to get the device.</summary>
    public bool CreateAggregateDevice(uint inputKind, out byte[] deviceIdOut)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi == null) { deviceIdOut = Array.Empty<byte>(); return false; }
        return GameInputInterop.CreateAggregateDevice(gi, inputKind, out deviceIdOut);
    }

    /// <summary>Disables an aggregate device by its 32-byte ID. All-zero device IDs are rejected. See docs/SECURITY.md.</summary>
    public bool DisableAggregateDevice(byte[] deviceIdBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        return gi != null && GameInputInterop.DisableAggregateDevice(gi, deviceIdBytes);
    }

    /// <summary>Creates a wait handle for the GameInput dispatcher. Wait on it instead of polling <see cref="DispatchCallbacks"/>; when signalled, call <see cref="DispatchCallbacks"/>. Dispose the returned object when done.</summary>
    /// <returns>A disposable wrapper holding the dispatcher and wait handle, or null if creation failed.</returns>
    public DispatcherWaitHandle? CreateDispatcherWaitHandle()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi == null) return null;
        if (!GameInputInterop.CreateDispatcherWaitHandle(gi, out IntPtr dispatcherPtr, out IntPtr waitHandlePtr))
            return null;
        return new DispatcherWaitHandle(dispatcherPtr, waitHandlePtr);
    }

    /// <summary>Dispatches pending device and reading callbacks (call from game loop). Raises <see cref="DeviceCallback"/> and <see cref="ReadingCallback"/> for each. Call from a single thread only; the manager is not thread-safe.</summary>
    /// <param name="quotaMicroseconds">Max time to spend in the native dispatcher (e.g. 1000).</param>
    public void DispatchCallbacks(ulong quotaMicroseconds = 1000)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi == null) return;

        if (!_deviceCallbackRegistered && DeviceCallback != null)
        {
            _deviceCallbackHandle = GCHandle.Alloc(_deviceCallbackQueue);
            if (GameInputInterop.RegisterDeviceCallbackAsync(gi, _deviceCallbackHandle, out _deviceCallbackToken))
                _deviceCallbackRegistered = true;
            else
                _deviceCallbackHandle.Free();
        }

        if (_deviceCallbackRegistered)
            GameInputInterop.DispatchCallbacks(gi, quotaMicroseconds);

        while (_deviceCallbackQueue.TryDequeue(out var ev))
        {
            _inCallback = true;
            try { DeviceCallback?.Invoke(this, new DeviceCallbackEventArgs(ev.Item1, ev.Item2, ev.Item3, ev.Item4)); }
            finally { _inCallback = false; }
        }

        while (_readingCallbackQueue.TryDequeue(out var ev))
        {
            var (readingPtr, hasOverrun) = ev;
            if (readingPtr != IntPtr.Zero && ReadingCallback != null)
            {
                var handle = new GameInputReadingHandle(readingPtr, p => GameInputInterop.ReleaseReading(p));
                _inCallback = true;
                try { ReadingCallback.Invoke(this, new ReadingCallbackEventArgs(handle, hasOverrun)); }
                catch { handle.Dispose(); }
                finally { _inCallback = false; }
            }
            else if (readingPtr != IntPtr.Zero)
                GameInputInterop.ReleaseReading(readingPtr);
        }

        while (_systemButtonQueue.TryDequeue(out _))
        {
            _inCallback = true;
            try { SystemButtonPressed?.Invoke(this, EventArgs.Empty); }
            finally { _inCallback = false; }
        }
        while (_keyboardLayoutQueue.TryDequeue(out _))
        {
            _inCallback = true;
            try { KeyboardLayoutChanged?.Invoke(this, EventArgs.Empty); }
            finally { _inCallback = false; }
        }
    }

    /// <summary>Registers for reading callbacks for the given device and input kind. Call <see cref="DispatchCallbacks"/> to pump; <see cref="ReadingCallback"/> will be raised. Dispose the reading handle in the handler.</summary>
    /// <param name="device">The device to receive readings for (use <see cref="IInputDevice.GetDevicePointer"/> if passing a raw pointer from elsewhere).</param>
    /// <param name="inputKind">Input kind (e.g. <see cref="GameInputKinds.Gamepad"/>, <see cref="GameInputKinds.Keyboard"/>).</param>
    /// <returns>True if registration succeeded.</returns>
    public bool RegisterReadingCallback(IInputDevice? device, uint inputKind)
    {
        return RegisterReadingCallback(device, inputKind, out _);
    }

    /// <summary>Registers for reading callbacks and returns the callback token for use with <see cref="StopCallback"/>.</summary>
    /// <param name="device">The device to receive readings for.</param>
    /// <param name="inputKind">Input kind (e.g. <see cref="GameInputKinds.Gamepad"/>).</param>
    /// <param name="callbackToken">Receives the token to pass to <see cref="StopCallback"/> for fire-and-forget stop, or <see cref="UnregisterCallback(ulong)"/> to wait for completion.</param>
    public bool RegisterReadingCallback(IInputDevice? device, uint inputKind, out ulong callbackToken)
    {
        callbackToken = 0;
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (device == null) return false;
        var gi = GetOrCreateGameInput();
        if (gi == null) return false;
        IntPtr ptr = device.GetDevicePointer();
        if (ptr == IntPtr.Zero) return false;
        if (!_readingCallbackHandleAllocated)
        {
            _readingCallbackHandle = GCHandle.Alloc(_readingCallbackQueue);
            _readingCallbackHandleAllocated = true;
        }
        if (!GameInputInterop.RegisterReadingCallback(gi, ptr, inputKind, _readingCallbackHandle, out ulong token))
        {
            _logger.LogDebug("RegisterReadingCallback failed for device {DeviceId} inputKind {InputKind}.", device.DeviceId, inputKind);
            return false;
        }
        callbackToken = token;
        lock (_readingCallbackTokens) { _readingCallbackTokens.Add(token); }
        return true;
    }

    /// <summary>Registers for system button (e.g. Xbox guide) callbacks. Call <see cref="DispatchCallbacks"/> to pump; <see cref="SystemButtonPressed"/> is raised.</summary>
    public bool RegisterSystemButtonCallback(IInputDevice? device, uint buttonFilter)
    {
        return RegisterSystemButtonCallback(device, buttonFilter, out _);
    }

    /// <summary>Registers for system button callbacks and returns the callback token for use with <see cref="StopCallback"/>.</summary>
    /// <param name="device">The device to receive system button events for.</param>
    /// <param name="buttonFilter">Button filter (0 = all).</param>
    /// <param name="callbackToken">Receives the token to pass to <see cref="StopCallback"/> or <see cref="UnregisterCallback(ulong)"/>.</param>
    public bool RegisterSystemButtonCallback(IInputDevice? device, uint buttonFilter, out ulong callbackToken)
    {
        callbackToken = 0;
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (device == null) return false;
        var gi = GetOrCreateGameInput();
        if (gi == null) return false;
        IntPtr ptr = device.GetDevicePointer();
        if (ptr == IntPtr.Zero) return false;
        if (!_systemButtonHandleAllocated)
        {
            _systemButtonHandle = GCHandle.Alloc(_systemButtonQueue);
            _systemButtonHandleAllocated = true;
        }
        if (!GameInputInterop.RegisterSystemButtonCallback(gi, ptr, buttonFilter, _systemButtonHandle, out ulong token))
        {
            _logger.LogDebug("RegisterSystemButtonCallback failed for device {DeviceId} buttonFilter {ButtonFilter}.", device.DeviceId, buttonFilter);
            return false;
        }
        callbackToken = token;
        lock (_systemButtonTokens) { _systemButtonTokens.Add(token); }
        return true;
    }

    /// <summary>Registers for keyboard layout change callbacks. Call <see cref="DispatchCallbacks"/> to pump; <see cref="KeyboardLayoutChanged"/> is raised.</summary>
    public bool RegisterKeyboardLayoutCallback(IInputDevice? device)
    {
        return RegisterKeyboardLayoutCallback(device, out _);
    }

    /// <summary>Registers for keyboard layout callbacks and returns the callback token for use with <see cref="StopCallback"/>.</summary>
    /// <param name="device">The keyboard device.</param>
    /// <param name="callbackToken">Receives the token to pass to <see cref="StopCallback"/> or <see cref="UnregisterCallback(ulong)"/>.</param>
    public bool RegisterKeyboardLayoutCallback(IInputDevice? device, out ulong callbackToken)
    {
        callbackToken = 0;
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (device == null) return false;
        var gi = GetOrCreateGameInput();
        if (gi == null) return false;
        IntPtr ptr = device.GetDevicePointer();
        if (ptr == IntPtr.Zero) return false;
        if (!_keyboardLayoutHandleAllocated)
        {
            _keyboardLayoutHandle = GCHandle.Alloc(_keyboardLayoutQueue);
            _keyboardLayoutHandleAllocated = true;
        }
        if (!GameInputInterop.RegisterKeyboardLayoutCallback(gi, ptr, _keyboardLayoutHandle, out ulong token))
        {
            _logger.LogDebug("RegisterKeyboardLayoutCallback failed for device {DeviceId}.", device.DeviceId);
            return false;
        }
        callbackToken = token;
        lock (_keyboardLayoutTokens) { _keyboardLayoutTokens.Add(token); }
        return true;
    }

    /// <summary>Stops a callback without waiting for in-flight callbacks. Use <see cref="UnregisterCallback"/> to wait for completion. Pass a token from Register* overloads.</summary>
    public void StopCallback(ulong callbackToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gi = GetOrCreateGameInput();
        if (gi != null) GameInputInterop.StopCallback(gi, callbackToken);
    }

    /// <summary>Unregisters a callback and waits for any in-flight callbacks to complete. Pass a token from Register* overloads. Do not call from inside that same callback handler.</summary>
    /// <exception cref="InvalidOperationException">Thrown if called from inside a DeviceCallback or ReadingCallback handler (re-entrancy guard).</exception>
    public bool UnregisterCallback(ulong callbackToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_inCallback)
            throw new InvalidOperationException("Do not call UnregisterCallback from inside a DeviceCallback or ReadingCallback handler. Unregister from your main loop after the callback returns. See docs/SECURITY.md.");
        var gi = GetOrCreateGameInput();
        return gi != null && GameInputInterop.UnregisterCallback(gi, callbackToken);
    }

    /// <summary>Gets the device pointer from a reading. Caller must release the returned pointer when done (e.g. wrap in a device and dispose, or release the native reference).</summary>
    public IntPtr GetDeviceFromReading(GameInputReadingHandle reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null) return IntPtr.Zero;
        return GameInputInterop.GetDeviceFromReading(reading.UnsafePointer);
    }

    /// <summary>Gets the number of controller axes in the reading.</summary>
    public uint GetControllerAxisCount(GameInputReadingHandle reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading == null ? 0 : GameInputInterop.GetControllerAxisCount(reading.UnsafePointer);
    }

    /// <summary>Gets controller axis state (floats). Returns number of values written.</summary>
    public uint GetControllerAxisState(GameInputReadingHandle reading, float[] buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading == null || buffer == null ? 0 : GameInputInterop.GetControllerAxisState(reading.UnsafePointer, buffer);
    }

    /// <summary>Gets the number of controller buttons in the reading.</summary>
    public uint GetControllerButtonCount(GameInputReadingHandle reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading == null ? 0 : GameInputInterop.GetControllerButtonCount(reading.UnsafePointer);
    }

    /// <summary>Gets controller button state (uints). Returns number of values written.</summary>
    public uint GetControllerButtonState(GameInputReadingHandle reading, uint[] buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading == null || buffer == null ? 0 : GameInputInterop.GetControllerButtonState(reading.UnsafePointer, buffer);
    }

    /// <summary>Gets the number of controller switches in the reading.</summary>
    public uint GetControllerSwitchCount(GameInputReadingHandle reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading == null ? 0 : GameInputInterop.GetControllerSwitchCount(reading.UnsafePointer);
    }

    /// <summary>Gets controller switch state (positions as int). Returns number of values written.</summary>
    public uint GetControllerSwitchState(GameInputReadingHandle reading, int[] buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading == null || buffer == null ? 0 : GameInputInterop.GetControllerSwitchState(reading.UnsafePointer, buffer);
    }

    /// <summary>Gets the raw report pointer for the reading. Do not release; valid only for the lifetime of the reading.</summary>
    public bool GetRawReportFromReading(GameInputReadingHandle reading, out IntPtr reportPtr)
    {
        reportPtr = IntPtr.Zero;
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading != null && GameInputInterop.GetRawReportFromReading(reading.UnsafePointer, out reportPtr);
    }

    /// <summary>Gets gamepad state from a reading (e.g. from <see cref="ReadingCallback"/>). Returns null if the reading is not gamepad or fails.</summary>
    public GamepadState? GetGamepadStateFromReading(GameInputReadingHandle? reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null) return null;
        if (!GameInputInterop.GetGamepadStateFromReading(reading.UnsafePointer, out var native))
            return null;
        return new GamepadState
        {
            Buttons = native.Buttons,
            LeftTrigger = native.LeftTrigger,
            RightTrigger = native.RightTrigger,
            LeftThumbstickX = native.LeftThumbstickX,
            LeftThumbstickY = native.LeftThumbstickY,
            RightThumbstickX = native.RightThumbstickX,
            RightThumbstickY = native.RightThumbstickY
        };
    }

    /// <summary>Gets mouse state from a reading (e.g. from <see cref="ReadingCallback"/>). Returns null if the reading is not mouse or fails.</summary>
    public MouseState? GetMouseStateFromReading(GameInputReadingHandle? reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null) return null;
        if (!GameInputInterop.GetMouseStateFromReading(reading.UnsafePointer, out var native))
            return null;
        return new MouseState
        {
            Buttons = native.Buttons,
            PositionX = native.PositionX,
            PositionY = native.PositionY,
            AbsolutePositionX = native.AbsolutePositionX,
            AbsolutePositionY = native.AbsolutePositionY,
            WheelX = native.WheelX,
            WheelY = native.WheelY
        };
    }

    /// <summary>Gets keyboard state from a reading (e.g. from <see cref="ReadingCallback"/>). Returns empty array if not keyboard or fails.</summary>
    /// <param name="reading">The reading handle (e.g. from <see cref="ReadingCallbackEventArgs.Reading"/>).</param>
    /// <param name="maxKeys">Maximum number of keys to return (default 256). Capped at 1024. See docs/SECURITY.md.</param>
    public KeyState[] GetKeyboardStateFromReading(GameInputReadingHandle? reading, int maxKeys = 256)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null || maxKeys <= 0) return Array.Empty<KeyState>();
        maxKeys = Math.Min(maxKeys, 1024);  // Security: cap to reduce DoS (docs/SECURITY.md)
        uint keyCount = GameInputInterop.GetKeyCountFromReading(reading.UnsafePointer);
        if (keyCount == 0) return Array.Empty<KeyState>();
        int toRead = (int)Math.Min(keyCount, (uint)maxKeys);
        var nativeKeys = new Core.Native.GameInputKeyState[toRead];
        uint written = GameInputInterop.GetKeyStateFromReading(reading.UnsafePointer, nativeKeys);
        var result = new KeyState[written];
        for (int i = 0; i < written; i++)
        {
            result[i] = new KeyState
            {
                ScanCode = nativeKeys[i].ScanCode,
                CodePoint = nativeKeys[i].CodePoint,
                VirtualKey = nativeKeys[i].VirtualKey,
                IsDeadKey = nativeKeys[i].IsDeadKey
            };
        }
        return result;
    }

    /// <summary>Gets sensors (motion) state from a reading. Returns null if the reading does not contain sensors data or fails.</summary>
    public SensorsState? GetSensorsStateFromReading(GameInputReadingHandle? reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null) return null;
        if (!GameInputInterop.GetSensorsStateFromReading(reading.UnsafePointer, out var native))
            return null;
        return new SensorsState
        {
            AccelerationInGX = native.AccelerationInGX, AccelerationInGY = native.AccelerationInGY, AccelerationInGZ = native.AccelerationInGZ,
            AngularVelocityInRadPerSecX = native.AngularVelocityInRadPerSecX, AngularVelocityInRadPerSecY = native.AngularVelocityInRadPerSecY, AngularVelocityInRadPerSecZ = native.AngularVelocityInRadPerSecZ,
            HeadingInDegreesFromMagneticNorth = native.HeadingInDegreesFromMagneticNorth, HeadingAccuracy = native.HeadingAccuracy,
            OrientationW = native.OrientationW, OrientationX = native.OrientationX, OrientationY = native.OrientationY, OrientationZ = native.OrientationZ
        };
    }

    /// <summary>Gets arcade stick state from a reading. Returns null if the reading does not contain arcade stick data or fails.</summary>
    public ArcadeStickState? GetArcadeStickStateFromReading(GameInputReadingHandle? reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null) return null;
        if (!GameInputInterop.GetArcadeStickStateFromReading(reading.UnsafePointer, out var native))
            return null;
        return new ArcadeStickState { Buttons = native.Buttons };
    }

    /// <summary>Gets flight stick state from a reading. Returns null if the reading does not contain flight stick data or fails.</summary>
    public FlightStickState? GetFlightStickStateFromReading(GameInputReadingHandle? reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null) return null;
        if (!GameInputInterop.GetFlightStickStateFromReading(reading.UnsafePointer, out var native))
            return null;
        return new FlightStickState
        {
            Buttons = native.Buttons, HatSwitch = native.HatSwitch,
            Roll = native.Roll, Pitch = native.Pitch, Yaw = native.Yaw, Throttle = native.Throttle
        };
    }

    /// <summary>Gets racing wheel state from a reading. Returns null if the reading does not contain racing wheel data or fails.</summary>
    public RacingWheelState? GetRacingWheelStateFromReading(GameInputReadingHandle? reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (reading == null) return null;
        if (!GameInputInterop.GetRacingWheelStateFromReading(reading.UnsafePointer, out var native))
            return null;
        return new RacingWheelState
        {
            Buttons = native.Buttons, PatternShifterGear = native.PatternShifterGear,
            Wheel = native.Wheel, Throttle = native.Throttle, Brake = native.Brake, Clutch = native.Clutch, Handbrake = native.Handbrake
        };
    }

    /// <summary>Gets the timestamp of a reading in microseconds (for input latency or replay). Use with <see cref="GetGamepadStateFromReading"/> etc. to pair state with time.</summary>
    /// <param name="reading">The reading handle (e.g. from <see cref="ReadingCallbackEventArgs.Reading"/>).</param>
    /// <returns>Timestamp in microseconds, or 0 if reading is null or the call fails.</returns>
    public ulong GetReadingTimestamp(GameInputReadingHandle? reading)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return reading == null ? 0 : GameInputInterop.GetReadingTimestamp(reading.UnsafePointer);
    }

    /// <summary>Raised when a system button (e.g. Xbox guide) is pressed (if registered).</summary>
    public event EventHandler? SystemButtonPressed;

    /// <summary>Raised when the keyboard layout changes (if registered).</summary>
    public event EventHandler? KeyboardLayoutChanged;

    private readonly ConcurrentQueue<object> _systemButtonQueue = new();
    private GCHandle _systemButtonHandle;
    private bool _systemButtonHandleAllocated;
    private readonly List<ulong> _systemButtonTokens = new();
    private readonly ConcurrentQueue<object> _keyboardLayoutQueue = new();
    private GCHandle _keyboardLayoutHandle;
    private bool _keyboardLayoutHandleAllocated;
    private readonly List<ulong> _keyboardLayoutTokens = new();

    private Core.Native.IGameInput? GetOrCreateGameInput()
    {
        if (_gameInput != null)
            return _gameInput;
        _gameInput = GameInputInterop.TryCreateGameInput(_loadOnlyFromSystem32);
        return _gameInput;
    }

    /// <summary>Disposes the manager and releases native resources. Do not call from inside a <see cref="DeviceCallback"/> or <see cref="ReadingCallback"/> handler.</summary>
    /// <exception cref="InvalidOperationException">Thrown if called from inside a callback handler (re-entrancy guard).</exception>
    public void Dispose()
    {
        if (_disposed) return;
        if (_inCallback)
            throw new InvalidOperationException("Do not call Dispose from inside a DeviceCallback or ReadingCallback handler. See docs/SECURITY.md.");
        _disposed = true;
        var gi = _gameInput;
        if (gi != null)
        {
            if (_deviceCallbackRegistered)
            {
                GameInputInterop.UnregisterDeviceCallback(gi, _deviceCallbackToken);
                if (_deviceCallbackHandle.IsAllocated) _deviceCallbackHandle.Free();
            }
            lock (_readingCallbackTokens)
            {
                foreach (ulong t in _readingCallbackTokens)
                    GameInputInterop.UnregisterCallback(gi, t);
            }
            if (_readingCallbackHandleAllocated && _readingCallbackHandle.IsAllocated)
                _readingCallbackHandle.Free();
            lock (_systemButtonTokens)
            {
                foreach (ulong t in _systemButtonTokens)
                    GameInputInterop.UnregisterCallback(gi, t);
            }
            if (_systemButtonHandleAllocated && _systemButtonHandle.IsAllocated)
                _systemButtonHandle.Free();
            lock (_keyboardLayoutTokens)
            {
                foreach (ulong t in _keyboardLayoutTokens)
                    GameInputInterop.UnregisterCallback(gi, t);
            }
            if (_keyboardLayoutHandleAllocated && _keyboardLayoutHandle.IsAllocated)
                _keyboardLayoutHandle.Free();
        }
        _gameInput = null;
        GC.SuppressFinalize(this);
    }
}
