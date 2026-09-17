using LaunchMonitor.Proto;
using System.Globalization;
namespace ApproachR10Windows;
public sealed class MainForm:Form {
 readonly R10Service r10=new(); readonly Button connect=new(); readonly Label status=new(); readonly Label device=new(); readonly DataGridView shots=new(); int shotNumber;
 public MainForm(){Text="Approach R10 — Shot Tracker";Width=1100;Height=700;StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(24,24,28);ForeColor=Color.White;
  var top=new Panel{Dock=DockStyle.Top,Height=105,Padding=new Padding(16)};connect.Text="Connect R10";connect.Width=130;connect.Height=38;connect.Click+=async(_,_)=>{connect.Enabled=false;await r10.ConnectAsync();connect.Enabled=true;};top.Controls.Add(connect);
  status.Text="Not connected";status.AutoSize=true;status.Left=165;status.Top=10;status.ForeColor=Color.LightGray;top.Controls.Add(status);
  device.Text="Pair your R10 in Windows Bluetooth settings, then click Connect R10.";device.AutoSize=true;device.Left=165;device.Top=45;top.Controls.Add(device);Controls.Add(top);
  shots.Dock=DockStyle.Fill;shots.BackgroundColor=Color.FromArgb(30,30,34);shots.ForeColor=Color.White;shots.AllowUserToAddRows=false;shots.ReadOnly=true;shots.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
  foreach(var c in new[]{("#",50),("Club speed",0),("Ball speed",0),("Carry",0),("Launch",0),("Direction",0),("Spin",0),("Attack",0),("Shot type",0)}){shots.Columns.Add(c.Item1,c.Item1);shots.Columns[^1].Width=c.Item2;}
  Controls.Add(shots);r10.StatusChanged+=s=>BeginInvoke(()=>{status.Text=s;device.Text=$"{r10.Model??"Garmin Approach R10"}   Firmware: {r10.Firmware??"—"}   Battery: {(r10.Battery>0?r10.Battery+"%":"—")}";});r10.ShotReceived+=m=>BeginInvoke(()=>AddShot(m));FormClosing+=(_,_)=>r10.Dispose();
 }
 void AddShot(Metrics m){shotNumber++;double mph=2.236936292*Math.Max(0,m.BallMetrics?.BallSpeed??0);double clubMph=2.236936292*Math.Max(0,m.ClubMetrics?.ClubHeadSpeed??0);shots.Rows.Insert(0,shotNumber,clubMph>0?clubMph.ToString("0.0",CultureInfo.InvariantCulture):"—",mph>0?mph.ToString("0.0",CultureInfo.InvariantCulture):"—", "—",m.BallMetrics?.LaunchAngle.ToString("0.0")??"—",m.BallMetrics?.LaunchDirection.ToString("0.0")??"—",m.BallMetrics?.TotalSpin.ToString("0")??"—",m.ClubMetrics?.AttackAngle.ToString("0.0")??"—",m.ShotType.ToString());}
}
