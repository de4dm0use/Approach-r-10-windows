using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using InTheHand.Bluetooth;
using LaunchMonitor.Proto;
using gspro_r10.bluetooth;

namespace ApproachR10Windows;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PairedDevice> devices = new();
    private readonly ObservableCollection<ShotRow> shots = new();
    private LaunchMonitorDevice? monitor;
    private BluetoothDevice? bluetoothDevice;

    public MainWindow()
    {
        InitializeComponent();
        DeviceCombo.ItemsSource = devices;
        ShotsGrid.ItemsSource = shots;
        Loaded += async (_, _) => await ScanDevicesAsync();
        Closing += (_, _) => DisconnectMonitor();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await ScanDevicesAsync();

    private async Task ScanDevicesAsync()
    {
        try
        {
            SetStatus("Scanning paired Bluetooth devices…", false);
            devices.Clear();
            var paired = await Bluetooth.GetPairedDevicesAsync();
            foreach (var device in paired)
            {
                devices.Add(new PairedDevice(device));
            }

            var r10 = devices.FirstOrDefault(d =>
                string.Equals(d.Name, "Approach R10", StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains("R10", StringComparison.OrdinalIgnoreCase));
            if (r10 != null)
                DeviceCombo.SelectedItem = r10;

            AppendLog($"Found {devices.Count} paired Bluetooth device(s).");
            SetStatus(devices.Count == 0 ? "No paired devices" : "Ready to connect", false);
        }
        catch (Exception ex)
        {
            AppendLog($"Bluetooth scan failed: {ex.Message}");
            SetStatus("Bluetooth scan failed", false);
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceCombo.SelectedItem is not PairedDevice selected)
        {
            MessageBox.Show("Select your paired Approach R10 first.", "Approach R10", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DisconnectMonitor();
        bluetoothDevice = selected.Device;
        SetControls(connecting: true);
        SetStatus("Connecting…", false);
        DeviceInfo.Text = bluetoothDevice.Name;
        AppendLog($"Connecting to {bluetoothDevice.Name} ({bluetoothDevice.Id})…");

        try
        {
            monitor = new LaunchMonitorDevice(bluetoothDevice)
            {
                AutoWake = true,
                CalibrateTiltOnConnect = false,
                DebugLogging = false
            };

            monitor.Error += Monitor_Error;
            monitor.ReadinessChanged += Monitor_ReadinessChanged;
            monitor.ShotMetrics += Monitor_ShotMetrics;
            monitor.BatteryLifeUpdated += Monitor_BatteryLifeUpdated;

            bool success = await Task.Run(() => monitor.Setup());
            if (!success)
                throw new InvalidOperationException("The R10 setup sequence failed. Make sure the R10 is awake and paired with Windows.");

            SetStatus("Connected", true);
            ReadyText.Text = monitor.Ready ? "Yes" : "No";
            ModelText.Text = monitor.Model ?? "—";
            FirmwareText.Text = monitor.Firmware ?? "—";
            BatteryText.Text = $"{monitor.Battery}%";
            AppendLog("R10 connection and setup completed.");
        }
        catch (Exception ex)
        {
            AppendLog($"Connection failed: {ex.GetBaseException().Message}");
            SetStatus("Connection failed", false);
            DisconnectMonitor();
            MessageBox.Show(ex.GetBaseException().Message, "Approach R10 connection failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DisconnectButton_Click(object sender, RoutedEventArgs e) => DisconnectMonitor();

    private void Monitor_ShotMetrics(object sender, LaunchMonitorDevice.MetricsEventArgs e)
    {
        var metrics = e.Metrics;
        if (metrics == null) return;

        var ball = metrics.BallMetrics;
        var club = metrics.ClubMetrics;
        var spin = ball?.TotalSpin;
        var ballSpeedMph = ball == null ? null : ball.BallSpeed * 2.2369363f;
        var clubSpeedMph = club == null ? null : club.ClubHeadSpeed * 2.2369363f;

        Dispatcher.Invoke(() =>
        {
            BallSpeed.Text = ballSpeedMph.HasValue ? $"{ballSpeedMph.Value:0.0} mph" : "—";
            Launch.Text = ball == null ? "—" : $"{ball.LaunchAngle:0.0}° / {ball.LaunchDirection:+0.0;-0.0;0.0}°";
            Spin.Text = spin.HasValue ? $"{spin.Value:0} rpm" : "—";

            shots.Insert(0, new ShotRow(
                (int)metrics.ShotId,
                ballSpeedMph,
                clubSpeedMph,
                null,
                null,
                ball?.LaunchAngle,
                ball?.LaunchDirection,
                spin));

            while (shots.Count > 100)
                shots.RemoveAt(shots.Count - 1);

            AppendLog($"Shot {metrics.ShotId}: ball {Format(ballSpeedMph)} mph, club {Format(clubSpeedMph)} mph, launch {ball?.LaunchAngle:0.0}°, HLA {ball?.LaunchDirection:0.0}°, spin {spin:0} rpm");
        });
    }

    private void Monitor_ReadinessChanged(object sender, LaunchMonitorDevice.ReadinessChangedEventArgs e)
    {
        Dispatcher.Invoke(() => ReadyText.Text = e.Ready ? "Yes" : "No");
    }

    private void Monitor_BatteryLifeUpdated(object sender, BaseDevice.BatteryLifeUpdatedEventArgs e)
    {
        Dispatcher.Invoke(() => BatteryText.Text = $"{e.Battery}%");
    }

    private void Monitor_Error(object sender, LaunchMonitorDevice.ErrorEventArgs e)
    {
        Dispatcher.Invoke(() => AppendLog($"R10: {e.Severity} — {e.Message}"));
    }

    private void DisconnectMonitor()
    {
        if (monitor == null) return;
        try
        {
            monitor.Dispose();
        }
        catch (Exception ex)
        {
            AppendLog($"Disconnect warning: {ex.Message}");
        }
        monitor = null;
        bluetoothDevice = null;
        SetControls(connecting: false);
        SetStatus("Not connected", false);
        ReadyText.Text = "—";
        ModelText.Text = "—";
        FirmwareText.Text = "—";
        BatteryText.Text = "—";
    }

    private void SetControls(bool connecting)
    {
        ScanButton.IsEnabled = !connecting;
        ConnectButton.IsEnabled = !connecting;
        DisconnectButton.IsEnabled = connecting;
    }

    private void SetStatus(string text, bool connected)
    {
        StatusText.Text = text;
        StatusDot.Fill = connected ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) : new SolidColorBrush(Color.FromRgb(107, 114, 128));
    }

    private void AppendLog(string text)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }

    private static string Format(float? value) => value.HasValue ? value.Value.ToString("0.0") : "—";

    private sealed record PairedDevice(BluetoothDevice Device)
    {
        public string Name => Device.Name;
    }

    private sealed record ShotRow(
        int Number,
        float? BallSpeed,
        float? ClubSpeed,
        float? Carry,
        float? Total,
        float? LaunchAngle,
        float? LaunchDirection,
        float? Spin);
}
