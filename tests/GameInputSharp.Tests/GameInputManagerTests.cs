// Tests for GameInputManager and device enumeration.
// When GameInput runtime is not available, GetDevices() returns an empty list (no throw).
// Most tests do not require connected hardware; see docs/TEST_RESULTS.md.

using System.Linq;
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameInputSharp.Tests;

public class GameInputManagerTests
{
    [Trait("Category", "Hardware")]
    [Fact]
    public void GetDevices_ReturnsList_DoesNotThrow()
    {
        using var manager = new GameInputManager();
        var devices = manager.GetDevices();
        Assert.NotNull(devices);
        Assert.True(devices.Count >= 0);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void GetDevices_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.GetDevices());
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void GetDevices_CalledMultipleTimes_DoesNotThrow()
    {
        using var manager = new GameInputManager();
        var first = manager.GetDevices();
        var second = manager.GetDevices();
        Assert.NotNull(first);
        Assert.NotNull(second);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void Dispose_IsIdempotent_DoesNotThrow()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        manager.Dispose(); // second dispose must not throw
    }

    [Trait("Category", "CI")]
    [Fact]
    public void GetCurrentTimestamp_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.GetCurrentTimestamp());
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void GetCurrentTimestamp_WhenNotDisposed_ReturnsValue()
    {
        using var manager = new GameInputManager();
        ulong ts = manager.GetCurrentTimestamp();
        // With or without runtime: 0 or positive microseconds
        Assert.True(ts >= 0);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void TryGetDeviceByDeviceId_NullOrEmpty_ReturnsFalseAndNullDevice()
    {
        using var manager = new GameInputManager();
        bool foundNull = manager.TryGetDeviceByDeviceId(null, out var outNull);
        Assert.False(foundNull);
        Assert.Null(outNull);

        bool foundEmpty = manager.TryGetDeviceByDeviceId("", out var outEmpty);
        Assert.False(foundEmpty);
        Assert.Null(outEmpty);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void TryGetDeviceByDeviceId_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.TryGetDeviceByDeviceId("some-id", out _));
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void TryGetDeviceByDeviceId_NonExistentId_ReturnsFalse()
    {
        using var manager = new GameInputManager();
        bool found = manager.TryGetDeviceByDeviceId("0000000000000000000000000000000000000000000000000000000000000000", out var device);
        Assert.False(found);
        Assert.Null(device);
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void TryGetDeviceByDeviceId_WhenDeviceExists_ReturnsTrueAndDevice()
    {
        using var manager = new GameInputManager();
        var devices = manager.GetDevices();
        if (devices.Count == 0) return; // no hardware: skip assertion
        string id = devices[0].DeviceId;
        bool found = manager.TryGetDeviceByDeviceId(id, out var device);
        Assert.True(found);
        Assert.NotNull(device);
        Assert.Equal(id, device!.DeviceId);
        device.Dispose();
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void Gamepad_WhenConnected_GetCurrentGamepadState_ReturnsNonNull()
    {
        using var manager = new GameInputManager();
        var gamepad = manager.GetDevices().OfType<GamepadDevice>().FirstOrDefault();
        if (gamepad == null) return;
        try
        {
            GamepadState? state = manager.GetCurrentGamepadState(gamepad);
            Assert.NotNull(state);
        }
        finally
        {
            gamepad.Dispose();
        }
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void Gamepad_WhenConnected_PlayForceFeedbackConstant_ReturnsBool()
    {
        using var manager = new GameInputManager();
        var gamepad = manager.GetDevices().OfType<GamepadDevice>().FirstOrDefault();
        if (gamepad == null) return;
        try
        {
            _ = gamepad.PlayForceFeedbackConstant(0, 1000UL, 0.5f); // does not throw; return value is hardware-dependent
        }
        finally
        {
            gamepad.Dispose();
        }
    }

    [Trait("Category", "CI")]
    [Fact]
    public void GetCurrentGamepadState_NullGamepad_ReturnsNull()
    {
        using var manager = new GameInputManager();
        GamepadState? state = manager.GetCurrentGamepadState(null);
        Assert.Null(state);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void GetCurrentGamepadState_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.GetCurrentGamepadState(null));
    }

    [Trait("Category", "CI")]
    [Fact]
    public void GetCurrentMouseState_NullMouse_ReturnsNull()
    {
        using var manager = new GameInputManager();
        MouseState? state = manager.GetCurrentMouseState(null);
        Assert.Null(state);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void GetReadingTimestamp_NullReading_ReturnsZero()
    {
        using var manager = new GameInputManager();
        ulong ts = manager.GetReadingTimestamp(null);
        Assert.Equal(0UL, ts);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void GetReadingTimestamp_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.GetReadingTimestamp(null));
    }

    [Trait("Category", "CI")]
    [Fact]
    public void FindDeviceFromId_NullOrShortArray_ReturnsNull()
    {
        using var manager = new GameInputManager();
        Assert.Null(manager.FindDeviceFromId(null!));
        Assert.Null(manager.FindDeviceFromId(Array.Empty<byte>()));
        Assert.Null(manager.FindDeviceFromId(new byte[16]));
    }

    [Trait("Category", "CI")]
    [Fact]
    public void FindDeviceFromId_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        var id = new byte[32];
        Assert.Throws<ObjectDisposedException>(() => manager.FindDeviceFromId(id));
    }

    [Trait("Category", "CI")]
    [Fact]
    public void FindDeviceFromPlatformString_NullOrEmpty_ReturnsNull()
    {
        using var manager = new GameInputManager();
        Assert.Null(manager.FindDeviceFromPlatformString(null!));
        Assert.Null(manager.FindDeviceFromPlatformString(""));
    }

    [Trait("Category", "CI")]
    [Fact]
    public void FindDeviceFromPlatformString_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.FindDeviceFromPlatformString("some-string"));
    }

    [Trait("Category", "CI")]
    [Fact]
    public void RegisterReadingCallback_NullDevice_ReturnsFalse()
    {
        using var manager = new GameInputManager();
        bool ok = manager.RegisterReadingCallback(null, 0, out _);
        Assert.False(ok);
    }

    [Trait("Category", "CI")]
    [Fact]
    public void RegisterReadingCallback_AfterDispose_Throws()
    {
        var manager = new GameInputManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.RegisterReadingCallback(null, 0, out _));
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        using var manager = new GameInputManager(null);
        var devices = manager.GetDevices();
        Assert.NotNull(devices);
    }

    [Trait("Category", "Hardware")]
    [Fact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        using var manager = new GameInputManager(NullLogger.Instance);
        var devices = manager.GetDevices();
        Assert.NotNull(devices);
    }
}
