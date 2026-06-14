# Changelog

All notable product changes to **GameInputSharp.Core** are documented here.

This changelog intentionally excludes workflow, CI, publishing, and package-distribution automation changes. Those are repository operations, not package behavior.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [1.0.1] - 2026-06-14

### Fixed

- Treat unsupported GameInput COM interfaces as an unavailable runtime instead of throwing during initialization. This keeps `GetDevices()` and related APIs aligned with the documented behavior: if the GameInput runtime is missing or incompatible, calls return empty/null results rather than crashing.
- Validate null, empty, or short inputs in `FindDeviceFromId` and `FindDeviceFromPlatformString` before attempting GameInput initialization.

### Changed

- Removed documentation references that directed medical, simulation, or robotics use cases to a separate Enterprise package.
- Added a NuGet-specific package README without GitHub badges while keeping the repository README optimized for GitHub.

## [1.0.0] - 2026-06-14

### Added

- Added public and internal constants for GameInput raw/controller subflags:
  - `RawDeviceReport`
  - `ControllerAxis`
  - `ControllerButton`
  - `ControllerSwitch`

### Changed

- Promoted GameInputSharp.Core from alpha to stable version 1.0.0.
- Updated the Microsoft.GameInput dependency from 3.2.138 to 3.4.218.
- Updated the console sample to use Microsoft.GameInput 3.4.218.

### Security

- Capped native-derived extra axis/button counts at 1024 before allocating managed or native buffers.
- Limited `DirectInputEscape` input and output buffers to 64 KB each.
- Limited raw device report buffers to 8192 bytes.
- Rejected platform strings longer than 2048 characters.
- Capped keyboard `maxKeys` reads at 1024.
- Added a System32-only GameInput DLL loading option.
- Added callback re-entrancy guards that prevent `Dispose()` or `UnregisterCallback(ulong)` from being called inside a callback.
- Rejected all-zero device IDs for `FindDeviceFromId` and `DisableAggregateDevice`.

## [2026-02] - 2026-02

### Changed

- Switched initialization to the official `GameInputCreate(IGameInput**)` API.
- Fixed device enumeration to use an explicit device kind filter for common gamepad, controller, keyboard, and mouse devices.
- Aligned `GameInputDeviceInfo` layout and string encoding handling with Microsoft.GameInput documentation while avoiding unsafe display name and PNP path reads on PC runtimes.

### Added

- Added API alignment documentation for initialization, COM methods, struct layout, and future interop changes.
