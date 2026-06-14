// GameInputSharp.Core — P/Invoke and native constants for Microsoft GameInput (v3).
// This is a third-party C# wrapper library. It requires the official Microsoft.GameInput NuGet package.
// Not affiliated with, endorsed by, or supported by Microsoft Corporation. Intended for Windows PC development.

using System.Runtime.InteropServices;

namespace GameInputSharp.Core;

/// <summary>Native constants and P/Invoke for GameInput API (v3).</summary>
internal static partial class GameInputNative
{
    public const string DllName = "GameInput.dll";

    // GameInputKind (v3) — device/reading kind flags (see Microsoft GameInput docs)
    public const uint GameInputKindUnknown = 0x00000000;
    public const uint GameInputKindRawDeviceReport = 0x00000001;
    public const uint GameInputKindControllerAxis = 0x00000002;
    public const uint GameInputKindControllerButton = 0x00000004;
    public const uint GameInputKindControllerSwitch = 0x00000008;
    public const uint GameInputKindController = 0x0000000E;
    public const uint GameInputKindKeyboard = 0x00000010;
    public const uint GameInputKindMouse = 0x00000020;
    public const uint GameInputKindSensors = 0x00000040;
    public const uint GameInputKindArcadeStick = 0x00010000;
    public const uint GameInputKindFlightStick = 0x00020000;
    public const uint GameInputKindGamepad = 0x00040000;
    public const uint GameInputKindRacingWheel = 0x00080000;
    public const uint GameInputKindUiNavigation = 0x01000000;

    // GameInputFocusPolicy (for SetFocusPolicy)
    public const uint GameInputFocusPolicyDefault = 0;
    public const uint GameInputFocusPolicyBackground = 1;
    public const uint GameInputFocusPolicyExclusive = 2;

    // GameInputDeviceStatus
    public const uint GameInputDeviceNoStatus = 0x00000000;
    public const uint GameInputDeviceConnected = 0x00000001;
    public const uint GameInputDeviceAnyStatus = 0xFFFFFFFF;

    // GameInputEnumerationKind
    public const int GameInputNoEnumeration = 0;
    public const int GameInputAsyncEnumeration = 1;
    public const int GameInputBlockingEnumeration = 2;

    // IID_IGameInput (v3)
    public static readonly Guid IID_IGameInput = new("20EFC1C7-5D9A-43BA-B26F-B807FA48609C");

    // Official API: GameInputCreate(IGameInput** ppv). Use this instead of GameInputInitialize for PC/NuGet runtime.
    // [DllImport] not used; we resolve via GetExport after loading the DLL by path.
}
