using InTheHand.Bluetooth;
using LaunchMonitor.Proto;
using ApproachR10Windows.R10;
namespace ApproachR10Windows;
public sealed class R10Service : IDisposable {
    LaunchMonitorDevice? monitor; BluetoothDevice? device;
    public bool IsConnected=>monitor?.Device.Gatt.IsConnected==true;
    public string Status{get;private set;}="Not connected";
    public string? Model=>monitor?.Model; public string? Firmware=>monitor?.Firmware; public int Battery=>monitor?.Battery??0;
    public event Action<string>? StatusChanged; public event Action<Metrics>? ShotReceived;
    void SetStatus(string s){Status=s;StatusChanged?.Invoke(s);}
    public async Task ConnectAsync(string preferredName="Approach R10"){
        SetStatus("Searching paired Bluetooth devices…");
        var devices=await Bluetooth.GetPairedDevicesAsync();
        device=devices.FirstOrDefault(d=>string.Equals(d.Name,preferredName,StringComparison.OrdinalIgnoreCase)) ?? devices.FirstOrDefault(d=>d.Name?.Contains("R10",StringComparison.OrdinalIgnoreCase)==true);
        if(device==null){SetStatus("Approach R10 not found. Pair it in Windows Bluetooth settings first.");return;}
        try{
            SetStatus($"Connecting to {device.Name}…"); await device.Gatt.ConnectAsync();
            if(!device.Gatt.IsConnected){SetStatus("Bluetooth connection failed.");return;}
            monitor=new LaunchMonitorDevice(device); monitor.ShotReceived+=(m)=>ShotReceived?.Invoke(m); monitor.Error+=(e)=>SetStatus("R10: "+e);
            if(!await Task.Run(()=>monitor.Setup())){SetStatus("R10 setup/handshake failed.");monitor.Dispose();monitor=null;return;}

            // Match the reference adapter's default environment values. The R10
            // expects humidity as a normalized fraction and tee range in metres.
            // 60 F = 15.56 C, 100% relative humidity, sea-level altitude,
            // standard air density, and a 7 ft tee distance.
            if(!monitor.ShotConfig(15.5556f, 1f, 0f, 1f, 2.1336f))
                SetStatus("Connected, but R10 shot configuration was rejected.");
            else
                SetStatus($"Connected — {monitor.Model} / FW {monitor.Firmware} / Battery {monitor.Battery}% — Ready for shots");
        }catch(Exception ex){SetStatus("Connection error: "+ex.Message);monitor?.Dispose();monitor=null;}
    }
    public void Disconnect(){monitor?.Dispose();monitor=null;try{device?.Gatt.Disconnect();}catch{}device=null;SetStatus("Not connected");}
    public void Dispose()=>Disconnect();
}
