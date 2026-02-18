// COM interface definitions for GameInput v3. Vtable order must match native.
// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.
// Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

using System.Runtime.InteropServices;

namespace GameInputSharp.Core.Native;

[ComImport]
[Guid("20EFC1C7-5D9A-43BA-B26F-B807FA48609C")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGameInput
{
    [PreserveSig] ulong GetCurrentTimestamp();
    [PreserveSig] int GetCurrentReading(uint inputKind, IntPtr device, out IntPtr reading);
    [PreserveSig] int GetNextReading(IntPtr referenceReading, uint inputKind, IntPtr device, out IntPtr reading);
    [PreserveSig] int GetPreviousReading(IntPtr referenceReading, uint inputKind, IntPtr device, out IntPtr reading);
    [PreserveSig] int RegisterReadingCallback(IntPtr device, uint inputKind, IntPtr context, IntPtr callbackFunc, out ulong callbackToken);
    [PreserveSig] int RegisterDeviceCallback(IntPtr device, uint inputKind, uint statusFilter, int enumerationKind, IntPtr context, IntPtr callbackFunc, out ulong callbackToken);
    [PreserveSig] int RegisterSystemButtonCallback(IntPtr device, uint buttonFilter, IntPtr context, IntPtr callbackFunc, out ulong callbackToken);
    [PreserveSig] int RegisterKeyboardLayoutCallback(IntPtr device, IntPtr context, IntPtr callbackFunc, out ulong callbackToken);
    void StopCallback(ulong callbackToken);
    [PreserveSig] bool UnregisterCallback(ulong callbackToken);
    [PreserveSig] int CreateDispatcher(out IntPtr dispatcher);
    [PreserveSig] int FindDeviceFromId(in AppLocalDeviceId value, out IntPtr device);
    [PreserveSig] int FindDeviceFromPlatformString([MarshalAs(UnmanagedType.LPWStr)] string value, out IntPtr device);
    void SetFocusPolicy(uint policy);
    [PreserveSig] int CreateAggregateDevice(uint inputKind, out AppLocalDeviceId deviceId);
    [PreserveSig] int DisableAggregateDevice(in AppLocalDeviceId deviceId);
}

[ComImport]
[Guid("63E2F38B-A399-4275-8AE7-D4C6E524D12A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGameInputDevice
{
    [PreserveSig] int GetDeviceInfo(out IntPtr info);
    [PreserveSig] int GetHapticInfo(out GameInputHapticInfo info);
    [PreserveSig] uint GetDeviceStatus();
    [PreserveSig] int CreateForceFeedbackEffect(uint motorIndex, in GameInputForceFeedbackParamsUnion params_, out IntPtr effect);
    [PreserveSig] bool IsForceFeedbackMotorPoweredOn(uint motorIndex);
    void SetForceFeedbackMotorGain(uint motorIndex, float masterGain);
    void SetRumbleState(in GameInputRumbleParams rumbleParams);
    [PreserveSig] int DirectInputEscape(uint command, IntPtr bufferIn, uint bufferInSize, IntPtr bufferOut, uint bufferOutSize, out uint bufferOutSizeWritten);
    [PreserveSig] int CreateInputMapper(out IntPtr inputMapper);
    [PreserveSig] int GetExtraAxisCount(uint inputKind, out uint extraAxisCount);
    [PreserveSig] int GetExtraButtonCount(uint inputKind, out uint extraButtonCount);
    [PreserveSig] int GetExtraAxisIndexes(uint inputKind, uint extraAxisCount, IntPtr extraAxisIndexes);
    [PreserveSig] int GetExtraButtonIndexes(uint inputKind, uint extraButtonCount, IntPtr extraButtonIndexes);
    [PreserveSig] int CreateRawDeviceReport(uint reportId, int reportKind, out IntPtr report);
    [PreserveSig] int SendRawDeviceOutput(IntPtr report);
}

[ComImport]
[Guid("415EED2E-98CB-42C2-8F28-B94601074E31")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGameInputDispatcher
{
    [PreserveSig] bool Dispatch(ulong quotaInMicroseconds);
    [PreserveSig] int OpenWaitHandle(out IntPtr waitHandle);
}

[StructLayout(LayoutKind.Sequential)]
internal struct AppLocalDeviceId
{
    public const int Size = 32;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] Value;

    public static AppLocalDeviceId Create() => new() { Value = new byte[Size] };
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct GameInputHapticInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public char[] AudioEndpointId;
    public uint LocationCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public Guid[] Locations;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputForceFeedbackParams
{
    public int Kind;
    public GameInputForceFeedbackEnvelope Envelope;
    public GameInputForceFeedbackMagnitude Magnitude;
}

/// <summary>Constant effect: envelope + magnitude. Union member for Kind=Constant.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GameInputForceFeedbackConstantParams
{
    public GameInputForceFeedbackEnvelope Envelope;
    public GameInputForceFeedbackMagnitude Magnitude;
}

/// <summary>Ramp effect: envelope + start/end magnitude. Union member for Kind=Ramp.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GameInputForceFeedbackRampParams
{
    public GameInputForceFeedbackEnvelope Envelope;
    public GameInputForceFeedbackMagnitude StartMagnitude;
    public GameInputForceFeedbackMagnitude EndMagnitude;
}

/// <summary>Periodic effect: envelope + magnitude + frequency, phase, bias. Union member for Sine/Square/Triangle/Sawtooth.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GameInputForceFeedbackPeriodicParams
{
    public GameInputForceFeedbackEnvelope Envelope;
    public GameInputForceFeedbackMagnitude Magnitude;
    public float Frequency;
    public float Phase;
    public float Bias;
}

/// <summary>Condition effect: magnitude + coefficients. Union member for Spring/Friction/Damper/Inertia.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GameInputForceFeedbackConditionParams
{
    public GameInputForceFeedbackMagnitude Magnitude;
    public float PositiveCoefficient;
    public float NegativeCoefficient;
    public float MaxPositiveMagnitude;
    public float MaxNegativeMagnitude;
    public float DeadZone;
    public float Bias;
}

/// <summary>Full params with union for all effect kinds. Pass to CreateForceFeedbackEffect.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct GameInputForceFeedbackParamsUnion
{
    [FieldOffset(0)] public int Kind;
    [FieldOffset(4)] public GameInputForceFeedbackConstantParams Constant;
    [FieldOffset(4)] public GameInputForceFeedbackRampParams Ramp;
    [FieldOffset(4)] public GameInputForceFeedbackPeriodicParams Periodic;
    [FieldOffset(4)] public GameInputForceFeedbackConditionParams Condition;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputForceFeedbackEnvelope
{
    public ulong AttackDuration;
    public ulong SustainDuration;
    public ulong ReleaseDuration;
    public float AttackGain;
    public float SustainGain;
    public float ReleaseGain;
    public uint PlayCount;
    public ulong RepeatDelay;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputForceFeedbackMagnitude
{
    public float LinearX, LinearY, LinearZ;
    public float AngularX, AngularY, AngularZ;
    public float Normal;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputRumbleParams
{
    public float LowFrequency;
    public float HighFrequency;
    public float LeftTrigger;
    public float RightTrigger;
}

/// <summary>COM interface for a single input reading (v3).</summary>
[ComImport]
[Guid("C81C4CDE-ED1A-4631-A30F-C556A6241A1F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGameInputReading
{
    [PreserveSig] uint GetInputKind();
    [PreserveSig] ulong GetTimestamp();
    void GetDevice(out IntPtr device);
    [PreserveSig] uint GetControllerAxisCount();
    [PreserveSig] uint GetControllerAxisState(uint stateArrayCount, IntPtr stateArray);
    [PreserveSig] uint GetControllerButtonCount();
    [PreserveSig] uint GetControllerButtonState(uint stateArrayCount, IntPtr stateArray);
    [PreserveSig] uint GetControllerSwitchCount();
    [PreserveSig] uint GetControllerSwitchState(uint stateArrayCount, IntPtr stateArray);
    [PreserveSig] uint GetKeyCount();
    [PreserveSig] uint GetKeyState(uint stateArrayCount, IntPtr stateArray);
    [PreserveSig] bool GetMouseState(out GameInputMouseState state);
    [PreserveSig] bool GetSensorsState(out GameInputSensorsState state);
    [PreserveSig] bool GetArcadeStickState(out GameInputArcadeStickState state);
    [PreserveSig] bool GetFlightStickState(out GameInputFlightStickState state);
    [PreserveSig] bool GetGamepadState(out GameInputGamepadState state);
    [PreserveSig] bool GetRacingWheelState(out GameInputRacingWheelState state);
    [PreserveSig] bool GetRawReport(out IntPtr report);
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputGamepadState
{
    public uint Buttons;
    public float LeftTrigger;
    public float RightTrigger;
    public float LeftThumbstickX;
    public float LeftThumbstickY;
    public float RightThumbstickX;
    public float RightThumbstickY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputMouseState
{
    public uint Buttons;
    public uint Positions;
    public long PositionX;
    public long PositionY;
    public long AbsolutePositionX;
    public long AbsolutePositionY;
    public long WheelX;
    public long WheelY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputKeyState
{
    public uint ScanCode;
    public uint CodePoint;
    public byte VirtualKey;
    public bool IsDeadKey;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputSensorsState
{
    public float AccelerationInGX, AccelerationInGY, AccelerationInGZ;
    public float AngularVelocityInRadPerSecX, AngularVelocityInRadPerSecY, AngularVelocityInRadPerSecZ;
    public float HeadingInDegreesFromMagneticNorth;
    public uint HeadingAccuracy;
    public float OrientationW, OrientationX, OrientationY, OrientationZ;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputArcadeStickState
{
    public uint Buttons;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputFlightStickState
{
    public uint Buttons;
    public int HatSwitch;
    public float Roll, Pitch, Yaw, Throttle;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputRacingWheelState
{
    public uint Buttons;
    public int PatternShifterGear;
    public float Wheel, Throttle, Brake, Clutch, Handbrake;
}

/// <summary>Force feedback effect state.</summary>
internal enum GameInputFeedbackEffectState
{
    Stopped = 0,
    Running = 1,
    Paused = 2
}

/// <summary>Force feedback effect kind (matches GameInputForceFeedbackEffectKind).</summary>
internal enum GameInputForceFeedbackEffectKind
{
    Constant = 0,
    Ramp = 1,
    SineWave = 2,
    SquareWave = 3,
    TriangleWave = 4,
    SawtoothUpWave = 5,
    SawtoothDownWave = 6,
    Spring = 7,
    Friction = 8,
    Damper = 9,
    Inertia = 10
}

[ComImport]
[Guid("FF61096A-3373-4093-A1DF-6D31846B3511")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGameInputForceFeedbackEffect
{
    void GetDevice(out IntPtr device);
    [PreserveSig] uint GetMotorIndex();
    [PreserveSig] float GetGain();
    void SetGain(float gain);
    void GetParams(out GameInputForceFeedbackParamsUnion params_);
    [PreserveSig] bool SetParams(in GameInputForceFeedbackParamsUnion params_);
    [PreserveSig] int GetState();
    void SetState(int state);
}

// --- IGameInputMapper (vtable dispatch; no COM cast needed) ---
// Axis mapping: how a logical axis is mapped from controller elements (axis/button/switch).
[StructLayout(LayoutKind.Sequential)]
internal struct GameInputAxisMapping
{
    public int ControllerElementKind;
    public uint ControllerIndex;
    [MarshalAs(UnmanagedType.I1)]
    public bool IsInverted;
    [MarshalAs(UnmanagedType.I1)]
    public bool FromTwoButtons;
    public uint ButtonMinIndexValue;
    public int ReferenceDirection;
}

// Button mapping: how a logical button is mapped from controller elements.
[StructLayout(LayoutKind.Sequential)]
internal struct GameInputButtonMapping
{
    public int ControllerElementKind;
    public uint ControllerIndex;
    [MarshalAs(UnmanagedType.I1)]
    public bool IsInverted;
    public int SwitchPosition;
}

// --- IGameInputRawDeviceReport (v3) ---
internal enum GameInputRawDeviceReportKind
{
    InputReport = 0,
    OutputReport = 1
}

[StructLayout(LayoutKind.Sequential)]
internal struct GameInputRawDeviceReportInfo
{
    public GameInputRawDeviceReportKind Kind;
    public uint Id;
    public uint Size;
}

[ComImport]
[Guid("05A42D89-2CB6-45A3-874D-E635723587AB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGameInputRawDeviceReport
{
    [PreserveSig] int GetDevice(out IntPtr device);
    [PreserveSig] int GetReportInfo(out GameInputRawDeviceReportInfo reportInfo);
    [PreserveSig] UIntPtr GetRawDataSize();
    [PreserveSig] UIntPtr GetRawData(UIntPtr bufferSize, IntPtr buffer);
    [PreserveSig] bool SetRawData(UIntPtr bufferSize, IntPtr buffer);
}
