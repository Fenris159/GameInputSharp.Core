// Tests for device wrappers, haptics, and mapping constants (no hardware required for most).

using GameInputSharp.Abstractions;
using GameInputSharp.Haptics;
using Xunit;

namespace GameInputSharp.Tests;

public class DeviceWrapperTests
{
    [Fact]
    public void HapticLocation_Enum_HasExpectedValues()
    {
        Assert.Equal(0, (int)HapticLocation.LeftLowFrequency);
        Assert.Equal(1, (int)HapticLocation.RightHighFrequency);
        Assert.True((int)HapticLocation.Location7 >= 7);
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void IInputDevice_Contract_DisplayNameAndDeviceId_AreStrings()
    {
        using var manager = new GameInputManager();
        foreach (var d in manager.GetDevices())
        {
            Assert.NotNull(d.DisplayName);
            Assert.NotNull(d.DeviceId);
        }
    }

    [Fact]
    public void GameInputGamepadButtons_HasExpectedBitValues()
    {
        Assert.Equal(0x00000004u, (uint)GameInputGamepadButtons.A);
        Assert.Equal(0x00000008u, (uint)GameInputGamepadButtons.B);
        Assert.Equal(0x00000040u, (uint)GameInputGamepadButtons.DPadUp);
        Assert.Equal(0x00000000u, (uint)GameInputGamepadButtons.None);
    }

    [Fact]
    public void GamepadState_Buttons_CanBeTestedWithGameInputGamepadButtons()
    {
        // Simulate A pressed: bitmask has A bit set
        uint buttons = (uint)GameInputGamepadButtons.A;
        Assert.True((buttons & (uint)GameInputGamepadButtons.A) != 0);
        Assert.False((buttons & (uint)GameInputGamepadButtons.B) != 0);
    }

    [Fact]
    public void GameInputGamepadAxes_HasExpectedValues()
    {
        Assert.Equal(0x00000001u, (uint)GameInputGamepadAxes.LeftTrigger);
        Assert.Equal(0x00000020u, (uint)GameInputGamepadAxes.RightThumbstickY);
    }
}
