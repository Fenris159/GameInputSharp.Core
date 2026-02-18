// Phase 3: MonoGame game loop example — integrate GameInputSharp in Update/Draw.
// Run with: dotnet run (Windows; requires MonoGame and GameInput runtime).

using System;
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GameInputSharp.Samples.MonoGame;

public class GameInputMonoGameSample : Game
{
    private GameInputManager? _gameInputManager;
    private GraphicsDeviceManager? _graphics;

    public GameInputMonoGameSample()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _gameInputManager = new GameInputManager();
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        // Poll GameInput devices once per frame (alternative to MonoGame's GamePad class)
        if (_gameInputManager != null)
        {
            var devices = _gameInputManager.GetDevices();
            foreach (var d in devices)
            {
                if (d is GamepadDevice gamepad && gamepad.IsConnected)
                {
                    // Example: trigger rumble on A button (would need GetCurrentReading for real input)
                    // gamepad.Haptics.SetVibration(0.5f, 0.5f);
                }
            }
        }

        if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice?.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _gameInputManager?.Dispose();
        _gameInputManager = null;
        base.UnloadContent();
    }
}

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new GameInputMonoGameSample();
        game.Run();
    }
}
