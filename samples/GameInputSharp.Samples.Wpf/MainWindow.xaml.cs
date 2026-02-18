using System.Windows;
using System.Windows.Media;
using GameInputSharp.Abstractions;
using GameInputSharp.Devices;

namespace GameInputSharp.Samples.Wpf;

public partial class MainWindow : Window
{
    private GameInputManager? _manager;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += (_, _) => _manager?.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _manager = new GameInputManager();
        RefreshDevices();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void RefreshDevices()
    {
        if (_manager == null) return;
        DeviceList.Items.Clear();
        try
        {
            var devices = _manager.GetDevices();
            foreach (var d in devices)
            {
                string type = d switch
                {
                    GamepadDevice => "Gamepad",
                    KeyboardDevice => "Keyboard",
                    MouseDevice => "Mouse",
                    _ => "Device"
                };
                DeviceList.Items.Add($"{type}: {d.DisplayName} — {d.DeviceId}");
            }
            StatusText.Text = devices.Count == 0
                ? "No devices. Ensure GameInput runtime is installed and a controller/keyboard/mouse is connected."
                : $"{devices.Count} device(s) found.";
        }
        catch (System.Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            StatusText.Foreground = new SolidColorBrush(Colors.DarkRed);
        }
    }
}
