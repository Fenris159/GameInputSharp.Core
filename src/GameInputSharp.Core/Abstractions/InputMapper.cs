// Input mapper: query how axes and buttons are mapped from physical controller elements.
// Wraps IGameInputMapper from IGameInputDevice::CreateInputMapper.
// This is a third-party C# wrapper library. Not affiliated with Microsoft Corporation.

using System.Runtime.InteropServices;
using GameInputSharp.Core;
using GameInputSharp.Core.Native;

namespace GameInputSharp.Abstractions;

/// <summary>Describes how a logical axis is mapped from controller axes, buttons, or switches.</summary>
public readonly struct AxisMappingInfo
{
    /// <summary>Kind of controller element (axis, button, switch).</summary>
    public int ControllerElementKind { get; init; }

    /// <summary>Zero-based index of the source element.</summary>
    public uint ControllerIndex { get; init; }

    /// <summary>Whether the axis is inverted (valid when mapped from an axis).</summary>
    public bool IsInverted { get; init; }

    /// <summary>True when axis is mapped from two buttons (valid when mapped from buttons).</summary>
    public bool FromTwoButtons { get; init; }

    /// <summary>Button index for minimum value (valid when FromTwoButtons is true).</summary>
    public uint ButtonMinIndexValue { get; init; }

    /// <summary>Switch position for positive direction (valid when mapped from a switch).</summary>
    public int ReferenceDirection { get; init; }
}

/// <summary>Describes how a logical button is mapped from controller axes, buttons, or switches.</summary>
public readonly struct ButtonMappingInfo
{
    /// <summary>Kind of controller element (axis, button, switch).</summary>
    public int ControllerElementKind { get; init; }

    /// <summary>Zero-based index of the source element.</summary>
    public uint ControllerIndex { get; init; }

    /// <summary>Whether the axis value is inverted before translating to button (valid when from axis).</summary>
    public bool IsInverted { get; init; }

    /// <summary>Switch position that represents pressed (valid when mapped from a switch).</summary>
    public int SwitchPosition { get; init; }
}

/// <summary>Wrapper for IGameInputMapper. Use to query how axes and buttons are mapped. Dispose when done to release the native mapper.</summary>
/// <remarks>Create via GamepadDevice.CreateInputMapper, KeyboardDevice.CreateInputMapper, or MouseDevice.CreateInputMapper.</remarks>
public sealed class InputMapper : IDisposable
{
    private IntPtr _mapperPtr;
    private bool _disposed;

    internal InputMapper(IntPtr mapperPtr)
    {
        _mapperPtr = mapperPtr;
    }

    /// <summary>Whether the mapper is still valid (not disposed).</summary>
    public bool IsValid => _mapperPtr != IntPtr.Zero && !_disposed;

    /// <summary>Gets gamepad axis mapping info. Returns null if the device does not support gamepad or the axis is not supported.</summary>
    /// <param name="axisElement">Gamepad axis (e.g. <see cref="GameInputGamepadAxes.LeftThumbstickX"/>).</param>
    public AxisMappingInfo? GetGamepadAxisMappingInfo(int axisElement) =>
        GameInputInterop.TryGetGamepadAxisMappingInfo(_mapperPtr, axisElement, out var m) ? ToAxisMappingInfo(m) : null;

    /// <summary>Gets gamepad button mapping info.</summary>
    /// <param name="buttonElement">Gamepad button (e.g. <see cref="GameInputGamepadButtons.A"/>).</param>
    public ButtonMappingInfo? GetGamepadButtonMappingInfo(int buttonElement) =>
        GameInputInterop.TryGetGamepadButtonMappingInfo(_mapperPtr, buttonElement, out var m) ? ToButtonMappingInfo(m) : null;

    /// <summary>Gets flight stick axis mapping info.</summary>
    public AxisMappingInfo? GetFlightStickAxisMappingInfo(int axisElement) =>
        GameInputInterop.TryGetFlightStickAxisMappingInfo(_mapperPtr, axisElement, out var m) ? ToAxisMappingInfo(m) : null;

    /// <summary>Gets flight stick button mapping info.</summary>
    public ButtonMappingInfo? GetFlightStickButtonMappingInfo(int buttonElement) =>
        GameInputInterop.TryGetFlightStickButtonMappingInfo(_mapperPtr, buttonElement, out var m) ? ToButtonMappingInfo(m) : null;

    /// <summary>Gets racing wheel axis mapping info.</summary>
    public AxisMappingInfo? GetRacingWheelAxisMappingInfo(int axisElement) =>
        GameInputInterop.TryGetRacingWheelAxisMappingInfo(_mapperPtr, axisElement, out var m) ? ToAxisMappingInfo(m) : null;

    /// <summary>Gets racing wheel button mapping info.</summary>
    public ButtonMappingInfo? GetRacingWheelButtonMappingInfo(int buttonElement) =>
        GameInputInterop.TryGetRacingWheelButtonMappingInfo(_mapperPtr, buttonElement, out var m) ? ToButtonMappingInfo(m) : null;

    /// <summary>Gets arcade stick button mapping info (arcade stick has no axes).</summary>
    public ButtonMappingInfo? GetArcadeStickButtonMappingInfo(int buttonElement) =>
        GameInputInterop.TryGetArcadeStickButtonMappingInfo(_mapperPtr, buttonElement, out var m) ? ToButtonMappingInfo(m) : null;

    private static AxisMappingInfo ToAxisMappingInfo(GameInputAxisMapping m) =>
        new()
        {
            ControllerElementKind = m.ControllerElementKind,
            ControllerIndex = m.ControllerIndex,
            IsInverted = m.IsInverted,
            FromTwoButtons = m.FromTwoButtons,
            ButtonMinIndexValue = m.ButtonMinIndexValue,
            ReferenceDirection = m.ReferenceDirection
        };

    private static ButtonMappingInfo ToButtonMappingInfo(GameInputButtonMapping m) =>
        new()
        {
            ControllerElementKind = m.ControllerElementKind,
            ControllerIndex = m.ControllerIndex,
            IsInverted = m.IsInverted,
            SwitchPosition = m.SwitchPosition
        };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        if (_mapperPtr != IntPtr.Zero)
        {
            Marshal.Release(_mapperPtr);
            _mapperPtr = IntPtr.Zero;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
