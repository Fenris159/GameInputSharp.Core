# Troubleshooting

Common issues when using GameInputSharp.Core and how to fix them. Use this page together with [Compatibility](COMPATIBILITY.md) and [Distribution](DISTRIBUTION.md).

---

## No devices found (GetDevices() returns empty list)

### Init succeeded but count is 0

- **Cause:** GameInput runtime is loaded and working, but no devices are reported by the API on this machine.
- **What to do:**
  1. Try connecting a controller over **USB** instead of wireless. Some Xbox controllers (e.g. Elite) via the **Xbox Wireless Adapter** are exposed only as XInput or through Xbox Accessories; GameInput may not enumerate them on all Windows/driver setups.
  2. Ensure the controller is paired and recognized in **Windows Settings → Bluetooth & devices** (or USB).
  3. Open **Xbox Accessories** app to confirm the device is visible to Windows; GameInput may still not list it depending on driver stack.
  4. See [Compatibility — Controllers and connection](COMPATIBILITY.md#controllers-and-connection).

### Init failed (DLL not loaded or GameInputCreate failed)

- **Cause:** GameInput.dll / GameInputRedist.dll could not be loaded or the GameInput COM API failed to initialize.
- **What to do:**
  1. **Install or repair the GameInput runtime:** e.g. `winget install Microsoft.GameInput`, or install the runtime per [Microsoft’s redistribution rules](https://learn.microsoft.com/en-us/gaming/gdk/).
  2. **Visual C++ Redistributable:** If the DLL exists but load fails with Win32 error **126** (“The specified module could not be found”), a **dependency** of the DLL is usually missing — typically the [Visual C++ Redistributable (x64)](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist). Install it and try again.
  3. **Architecture:** Run a **64-bit** process if you’re using a 64-bit GameInput DLL (error **193** = wrong architecture).
  4. Use **diagnostics** (see below) to see which path was tried and what error was returned.

---

## How to get diagnostics

When init fails, use the manager’s diagnostic APIs **before** or right after the first failed `GetDevices()`:

```csharp
using var manager = new GameInputManager();
var (initOk, rawCount) = manager.GetDiagnostics();
if (!initOk)
{
    var (mainWin32, mainEx) = manager.GetMainPathLoadFailure();
    // mainWin32 = Win32 error code; mainEx = exception message from load attempt
    var (path1, path2, exists1, exists2, is64) = manager.GetLoadPaths();
    // path1/path2 = paths tried; exists1/exists2 = whether DLL existed
    var (win32Err, errMsg) = manager.GetLastLoadError();
}
```

- **GetMainPathLoadFailure** — Use first after init failure to see why the main load path failed.
- **GetLoadPaths** — Shows which paths were checked (e.g. System32, app directory) and whether the DLL existed.
- **GetLastLoadError** — Win32 error and message from a later load attempt.

Interpretation:

| Win32 code | Meaning |
|------------|--------|
| 2 | File not found |
| 5 | Access denied |
| 126 | Dependency missing (e.g. VC++ Redist x64) |
| 127 | Procedure not found in DLL |
| 193 | Wrong architecture (32 vs 64-bit) |

---

## NuGet / package not found

- **“Microsoft.GameInput not found” or restore errors:**  
  Add **Microsoft.GameInput** explicitly to your project (same version as in the wrapper, e.g. 3.4.218). The wrapper declares it as a dependency; NuGet should pull it from nuget.org when you install GameInputSharp.Core. If you’re on a private feed, ensure both packages are available.

- **“GameInputSharp.Core not found”:**  
  Ensure the package source that hosts GameInputSharp.Core is configured (e.g. nuget.org or your feed). For local development, use a local folder or `dotnet pack` and `dotnet add reference` to the built package.

---

## DLL load order and security

By default the wrapper tries: (1) System32, (2) application directory, (3) default search path. To **load only from System32** (e.g. to reduce DLL hijacking risk):

```csharp
using var manager = new GameInputManager(logger, loadOnlyFromSystem32: true);
```

If the DLL is not in System32, init fails and `GetDevices()` returns an empty list. See [Security & safety](SECURITY.md).

---

## Callbacks: “Do not call UnregisterCallback / Dispose from inside a callback”

If you call `UnregisterCallback(token)` or `manager.Dispose()` from inside a `DeviceCallback` or `ReadingCallback` handler, the wrapper throws `InvalidOperationException`. The native API does not allow unregistering from within the same callback.

**Fix:** Unregister or dispose from your **main loop** after the callback has returned (e.g. set a flag in the callback and call `UnregisterCallback` or shut down on the next frame).

---

## Rumble or force feedback does nothing

- Confirm the device supports haptics: e.g. `gamepad.GetHapticInfo()` and check motor count.
- Some devices expose multiple motors; use `SetVibration(left, right)` or advanced APIs for per-motor effects.
- Ensure the device is still connected; dispose and re-acquire after reconnect if needed.

---

## Unity / MonoGame / other engines

- **Unity:** See `samples/GameInputSharp.Samples.Unity/README.md` and [Compatibility — Unity](COMPATIBILITY.md#unity).
- **MonoGame:** Use the Windows project, create `GameInputManager` in `Initialize()`, poll in `Update()`. Sample: `samples/GameInputSharp.Samples.MonoGame`.
- **Godot / Stride / WPF:** See [Compatibility](COMPATIBILITY.md). Same APIs; reference the DLL and ensure the GameInput runtime is present on the target machine.

---

## FAQ

**Q: Do I need to ship GameInput.dll with my app?**  
A: The runtime (GameInput.dll / GameInputRedist.dll) must be available on the user’s machine — either inbox on Windows or installed via Microsoft’s redistribution. The wrapper does not bundle it. See [Distribution](DISTRIBUTION.md).

**Q: Can I use this on Linux or macOS?**  
A: The library builds on other targets but only enumerates devices on **Windows**; on non-Windows, `GetDevices()` returns an empty list.

**Q: Why do I get new device instances every time I call GetDevices()?**  
A: Each call returns new wrapper instances. Use `DeviceId` to correlate or `TryGetDeviceByDeviceId(deviceId)` to retrieve a device by ID. You own and should dispose device wrappers when done.

**Q: Where is the full API reference?**  
A: Use the XML documentation in your IDE (IntelliSense) or the assembly. This site covers [API constants and flags](API_REFERENCE.md), [mapping](MAPPING_REFERENCE.md), and [usage](USAGE.md); for every type/method, see the packaged DLL and XML.

