// Console sample — device enumeration, gamepad state, and optional rumble.

using System.Linq;
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

Console.WriteLine("GameInputSharp.Samples.Console — device enumeration and gamepad test.");

// Check that Microsoft.GameInput NuGet is in cache (rules out "package not restored" as cause of load failure)
const string GameInputPackageId = "microsoft.gameinput";
const string GameInputPackageVersion = "3.4.218";
string? packagesDir = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
if (string.IsNullOrEmpty(packagesDir))
    packagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
string packageDir = Path.Combine(packagesDir, GameInputPackageId, GameInputPackageVersion);
bool packageInCache = Directory.Exists(packageDir) && File.Exists(Path.Combine(packageDir, "microsoft.gameinput.nuspec"));
string packageStatus = packageInCache
    ? $"found at {packageDir}"
    : $"not found (expected under {Path.Combine(packagesDir, GameInputPackageId)}; run 'dotnet restore' from solution dir)";
Console.WriteLine($"NuGet cache: Microsoft.GameInput {GameInputPackageVersion} {packageStatus}");
Console.WriteLine();

using var manager = new GameInputManager();

var (initOk, rawCount) = manager.GetDiagnostics();
Console.WriteLine($"GameInput init: {(initOk ? "OK" : "Failed")}");
if (!initOk)
{
    // Main path failure (from first load attempt) — call first before GetLoadDiagnostics so we see the initial attempt’s result
    var (mainWin32, mainEx) = manager.GetMainPathLoadFailure();
    if (mainWin32 != 0 || !string.IsNullOrEmpty(mainEx))
    {
        string mainMsg = mainWin32 != 0 ? $"[{mainWin32}] {GetWin32Msg(mainWin32)}" : "";
        if (!string.IsNullOrEmpty(mainEx)) mainMsg += (string.IsNullOrEmpty(mainMsg) ? "" : " | ") + "Exception: " + mainEx;
        Console.WriteLine($"  Main path load failure: {mainMsg}");
    }
    else
        Console.WriteLine("  Main path: no load failure captured (DLL may have loaded but GameInputInitialize failed).");
    var (dllLoaded, initHr) = manager.GetLoadDiagnostics();
    Console.WriteLine($"  DLL loaded: {dllLoaded}");
    if (dllLoaded)
        Console.WriteLine($"  GameInputInitialize HRESULT: 0x{initHr:X8} (0 = success, -1 = no export)");
    else
    {
        var (path1, path2, exists1, exists2, is64) = manager.GetLoadPaths();
        Console.WriteLine($"  Process: {(is64 ? "64-bit" : "32-bit")}");
        Console.WriteLine($"  Checked: {path1}");
        Console.WriteLine($"    Exists: {exists1}");
        Console.WriteLine($"  Checked: {path2}");
        Console.WriteLine($"    Exists: {exists2}");
        if (exists1 || exists2)
        {
            var (win32Err, errMsg) = manager.GetLastLoadError();
            Console.WriteLine($"  Diagnostic load (later): [{win32Err}] {errMsg}");
        }
        else
            Console.WriteLine("  -> Install GameInput runtime; or copy GameInput.dll / GameInputRedist.dll to the path above.");
    }
}
else
    Console.WriteLine($"Raw devices from GameInput API: {rawCount}");
Console.WriteLine();

static string GetWin32Msg(int code) => code switch
{
    2 => "File not found.",
    5 => "Access denied.",
    126 => "Dependency missing (e.g. VC++ Redist x64).",
    127 => "Procedure not found in DLL.",
    193 => "Wrong architecture (32 vs 64-bit).",
    _ => $"Win32 {code}"
};

var devices = manager.GetDevices();
Console.WriteLine($"Devices found: {devices.Count}");
foreach (var d in devices)
{
    Console.WriteLine($"  - {d.DisplayName} (Id: {d.DeviceId})");
    if (d is GamepadDevice gamepad)
    {
        // Live state
        var state = manager.GetCurrentGamepadState(gamepad);
        if (state is { } s)
        {
            Console.WriteLine($"    [Gamepad] Buttons: 0x{s.Buttons:X8}  L/R Trigger: {s.LeftTrigger:F2} / {s.RightTrigger:F2}");
            Console.WriteLine($"    [Gamepad] LeftStick: ({s.LeftThumbstickX:F2}, {s.LeftThumbstickY:F2})  RightStick: ({s.RightThumbstickX:F2}, {s.RightThumbstickY:F2})");
        }
        // TryGetDeviceByDeviceId
        bool found = manager.TryGetDeviceByDeviceId(gamepad.DeviceId, out var byId);
        Console.WriteLine($"    [Gamepad] TryGetDeviceByDeviceId(DeviceId): {found}");
        if (byId != null) byId.Dispose();
        Console.WriteLine("    [Gamepad] Triggering short rumble.");
        gamepad.Haptics.SetVibration(0.25f, 0.25f);
        Thread.Sleep(300);
        gamepad.Haptics.SetVibration(0f, 0f);
    }
}
Console.WriteLine("Done.");
if (devices.Count == 0)
{
    if (!initOk)
        Console.WriteLine("  -> Install or repair the GameInput runtime (GameInput.dll / GameInputRedist.dll in System32).");
    else
        Console.WriteLine("  -> Init OK but no devices: your controller may be exposed only via XInput/Windows.Gaming.Input.");
    Console.WriteLine("  -> Xbox Elite via wireless adapter: try connecting the controller over USB to test; see docs/COMPATIBILITY.md.");
}
