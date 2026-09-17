using Google.Protobuf;
using InTheHand.Bluetooth;
using LaunchMonitor.Proto;
using static LaunchMonitor.Proto.State.Types;
namespace ApproachR10Windows.R10;
public sealed class LaunchMonitorDevice:BaseDevice {
 static readonly Guid MeasurementService=Guid.Parse("6A4E3400-667B-11E3-949A-0800200C9A66"),MeasurementCharacteristic=Guid.Parse("6A4E3401-667B-11E3-949A-0800200C9A66"),ControlCharacteristic=Guid.Parse("6A4E3402-667B-11E3-949A-0800200C9A66"),StatusCharacteristic=Guid.Parse("6A4E3403-667B-11E3-949A-0800200C9A66");
 readonly HashSet<uint> processed=new(); public StateType CurrentState{get;private set;} public bool Ready=>CurrentState==StateType.Waiting; public Tilt? DeviceTilt{get;private set;} public bool AutoWake{get;set;}=true; public bool CalibrateTiltOnConnect{get;set;}=false; public event Action<Metrics>? ShotReceived; public event Action<string>? Error;
 public LaunchMonitorDevice(BluetoothDevice d):base(d){}
 public override bool Setup(){var service=Device.Gatt.GetPrimaryServiceAsync(MeasurementService).WaitAsync(TimeSpan.FromSeconds(5)).Result;foreach(var id in new[]{MeasurementCharacteristic,ControlCharacteristic,StatusCharacteristic}){var c=service.GetCharacteristicAsync(id).WaitAsync(TimeSpan.FromSeconds(5)).Result;c.StartNotificationsAsync().Wait(TimeSpan.FromSeconds(5));}if(!base.Setup())return false;WakeDevice();CurrentState=StatusRequest()??StateType.Error;DeviceTilt=GetDeviceTilt();SubscribeToAlerts();if(CalibrateTiltOnConnect)StartTiltCalibration();return true;}
 public override void HandleProtobufRequest(IMessage request){if(request is not WrapperProto w||w.Event?.Notification==null)return;var n=w.Event.Notification;if(n?.State!=null){CurrentState=n.State.State_;if(CurrentState==StateType.Standby&&AutoWake)WakeDevice();}if(n?.Error!=null&&n.Error.HasCode)Error?.Invoke($"{n.Error.Code}: {n.Error.Severity}");if(n?.Metrics!=null&&!processed.Contains(n.Metrics.ShotId)){processed.Add(n.Metrics.ShotId);ShotReceived?.Invoke(n.Metrics);}if(n?.TiltCalibration!=null)DeviceTilt=GetDeviceTilt();}
 public Tilt? GetDeviceTilt(){var r=SendProtobufRequest(new WrapperProto{Service=new LaunchMonitorService{TiltRequest=new TiltRequest()}});return r is WrapperProto w?w.Service.TiltResponse.Tilt:null;}
 public StateType? StatusRequest(){var r=SendProtobufRequest(new WrapperProto{Service=new LaunchMonitorService{StatusRequest=new StatusRequest()}});return r is WrapperProto w?w.Service.StatusResponse.State.State_:null;}
 public WakeUpResponse.Types.ResponseStatus? WakeDevice(){var r=SendProtobufRequest(new WrapperProto{Service=new LaunchMonitorService{WakeUpRequest=new WakeUpRequest()}});return r is WrapperProto w?w.Service.WakeUpResponse.Status:null;}
 public bool ShotConfig(float temperature=20,float humidity=0.5f,float altitude=0,float airDensity=1,float teeRange=2.13f){var r=SendProtobufRequest(new WrapperProto{Service=new LaunchMonitorService{ShotConfigRequest=new ShotConfigRequest{Temperature=temperature,Humidity=humidity,Altitude=altitude,AirDensity=airDensity,TeeRange=teeRange}}});return r is WrapperProto w&&w.Service.ShotConfigResponse.Success;}
 public void SubscribeToAlerts(){SendProtobufRequest(new WrapperProto{Event=new EventSharing{SubscribeRequest=new SubscribeRequest{Alerts={new AlertMessage{Type=AlertNotification.Types.AlertType.LaunchMonitor}}}}});}
 public void StartTiltCalibration(){SendProtobufRequest(new WrapperProto{Service=new LaunchMonitorService{StartTiltCalRequest=new StartTiltCalibrationRequest()}});}
}
