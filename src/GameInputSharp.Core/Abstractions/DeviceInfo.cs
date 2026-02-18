// Full device information from IGameInputDevice::GetDeviceInfo (GameInputDeviceInfo).
// This is a third-party C# wrapper library. Not affiliated with Microsoft Corporation.

namespace GameInputSharp.Abstractions;

/// <summary>Four-part version (major, minor, build, revision). Used for hardware and firmware version.</summary>
public readonly struct GameInputVersion
{
    public ushort Major { get; init; }
    public ushort Minor { get; init; }
    public ushort Build { get; init; }
    public ushort Revision { get; init; }

    public override string ToString() => $"{Major}.{Minor}.{Build}.{Revision}";
}

/// <summary>HID usage (page + id). Describes the specific HID usage of the device.</summary>
public readonly struct GameInputUsage
{
    public ushort Page { get; init; }
    public ushort Id { get; init; }
}

/// <summary>Full device information from GameInputDeviceInfo. All string and pointer-derived fields are populated from the native struct; nested info pointers (e.g. gamepadInfo) are not dereferenced — only their presence is indicated.</summary>
public sealed class DeviceInfo
{
    /// <summary>Vendor ID (e.g. USB VID).</summary>
    public ushort VendorId { get; init; }

    /// <summary>Product ID (e.g. USB PID).</summary>
    public ushort ProductId { get; init; }

    /// <summary>Hardware revision number.</summary>
    public ushort RevisionNumber { get; init; }

    /// <summary>HID usage (page and id).</summary>
    public GameInputUsage Usage { get; init; }

    /// <summary>Hardware version (if any).</summary>
    public GameInputVersion HardwareVersion { get; init; }

    /// <summary>Firmware version (if any).</summary>
    public GameInputVersion FirmwareVersion { get; init; }

    /// <summary>Application-local device ID (32 bytes). Same value as used for DeviceId string.</summary>
    public byte[] DeviceId { get; init; } = Array.Empty<byte>();

    /// <summary>Root device ID for composite devices; same as DeviceId if not composite.</summary>
    public byte[] DeviceRootId { get; init; } = Array.Empty<byte>();

    /// <summary>Device family classification.</summary>
    public uint DeviceFamily { get; init; }

    /// <summary>Supported input kinds (GameInputKind flags).</summary>
    public uint SupportedInput { get; init; }

    /// <summary>Supported rumble motor flags.</summary>
    public uint SupportedRumbleMotors { get; init; }

    /// <summary>Supported system button flags.</summary>
    public uint SupportedSystemButtons { get; init; }

    /// <summary>Container ID (GUID).</summary>
    public Guid ContainerId { get; init; }

    /// <summary>Friendly display name (UTF-8 from native).</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Plug and Play path for the device.</summary>
    public string PnpPath { get; init; } = string.Empty;

    /// <summary>Whether keyboard info is available (pointer non-null).</summary>
    public bool HasKeyboardInfo { get; init; }

    /// <summary>Whether mouse info is available.</summary>
    public bool HasMouseInfo { get; init; }

    /// <summary>Whether sensors info is available.</summary>
    public bool HasSensorsInfo { get; init; }

    /// <summary>Whether generic controller info is available.</summary>
    public bool HasControllerInfo { get; init; }

    /// <summary>Whether arcade stick info is available.</summary>
    public bool HasArcadeStickInfo { get; init; }

    /// <summary>Whether flight stick info is available.</summary>
    public bool HasFlightStickInfo { get; init; }

    /// <summary>Whether gamepad info is available.</summary>
    public bool HasGamepadInfo { get; init; }

    /// <summary>Whether racing wheel info is available.</summary>
    public bool HasRacingWheelInfo { get; init; }

    /// <summary>Number of force feedback motors.</summary>
    public uint ForceFeedbackMotorCount { get; init; }

    /// <summary>Whether force feedback motor info array is available.</summary>
    public bool HasForceFeedbackMotorInfo { get; init; }

    /// <summary>Number of input reports.</summary>
    public uint InputReportCount { get; init; }

    /// <summary>Whether input report info is available.</summary>
    public bool HasInputReportInfo { get; init; }

    /// <summary>Number of output reports.</summary>
    public uint OutputReportCount { get; init; }

    /// <summary>Whether output report info is available.</summary>
    public bool HasOutputReportInfo { get; init; }
}
