using System.Diagnostics;
using System.Text.Json;
namespace BlakiBridge;
internal static class ResizeCheck
{
 internal static void Run(string output)
 {
  using var main=new LiveMain(diagnosticConfig:new BridgeConfig{Language="LT",OptimizeMouseSpeed=true}){Opacity=0,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=Screen.PrimaryScreen!.Bounds.Location};
  main.Show();Application.DoEvents();var results=new List<object>();
  foreach(int page in new[]{-1,0,1,2}){
   bool editor=page>=0; if(editor){main.EditDisplay(0);main.DisplayEditor!.SelectTab(page);}Application.DoEvents();int layouts=0;void Count(object? s,LayoutEventArgs e)=>layouts++;
   foreach(var row in Children(main).OfType<LayoutRow>())row.Layout+=Count;
   var times=new List<double>();
   typeof(LiveMain).GetMethod("OnResizeBegin",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main,[EventArgs.Empty]);
   for(int i=0;i<80;i++){var sw=Stopwatch.StartNew();main.Size=new Size(main.MinimumSize.Width+(i%20)*8,main.MinimumSize.Height+(i%20)*4);Application.DoEvents();times.Add(sw.Elapsed.TotalMilliseconds);}
   typeof(LiveMain).GetMethod("OnResizeEnd",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main,[EventArgs.Empty]);Application.DoEvents();
   if(main.ContentArea.Width!=main.ClientSize.Width)throw new InvalidOperationException("Final resize layout not applied");
   results.Add(new{page,editor,layouts,meanMs=times.Average(),p95Ms=times.Order().ElementAt(75),maxMs=times.Max()});
   foreach(var row in Children(main).OfType<LayoutRow>())row.Layout-=Count;
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{results}));main.Close();
 }
 internal static void Navigation(string output)
 {
  var config=new BridgeConfig{Language="LT",OptimizeMouseSpeed=true};
  using var main=new LiveMain(diagnosticConfig:config){Opacity=0,ShowInTaskbar=false};main.Show();Application.DoEvents();int checks=0;
  void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;}
  void Mouse(string method,Point point)=>typeof(MonitorCanvas).GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main.CanvasControl,[new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)]);
  Check(Children(main.ScreensPage).OfType<PageTabButton>().Count()==3,"Three tabs on main");
  for(int i=0;i<main.CanvasControl.Monitors.Count;i++){
   main.Refresh();Application.DoEvents();var r=main.CanvasControl.LabelMeasurements[i].Bounds;var point=new Point(r.Left+r.Width/2,r.Top+r.Height/2);
   Mouse("OnMouseDown",point);Mouse("OnMouseUp",point);Application.DoEvents();
   Check(main.DisplayEditor==null && main.ScreensPage.Visible,"Single click selects without leaving diagram "+i);
   var tabs=Children(main.ScreensPage).OfType<PageTabButton>().ToArray();
   Check(tabs.All(t=>t.Enabled),"Selection enables tabs");
   tabs[0].PerformClick();Application.DoEvents();
   Check(main.DisplayEditor!=null,"Tab opens selected monitor "+i);
   Check(main.DisplayEditor!.Preview.MonitorName==main.CanvasControl.Monitors[i].ModelName,"Selected monitor identity");
   main.DisplayEditor.SpeedInput.Value=123;main.DisplayEditor.Back.PerformClick();Application.DoEvents();
   Check(main.DisplayEditor==null && config.MouseSpeedPreview.Count==0 && config.MouseSpeedPercent.GetValueOrDefault(main.CanvasControl.Monitors[i].DeviceName,100)==100,"Back discards draft");
  }
  main.Refresh();var bounds=main.CanvasControl.LabelMeasurements[0].Bounds;var start=new Point(bounds.Right-6,bounds.Bottom-6);var end=new Point(start.X-12,start.Y-4);
  Mouse("OnMouseDown",start);Mouse("OnMouseMove",end);Mouse("OnMouseUp",end);Application.DoEvents();
  Check(main.DisplayEditor==null,"Resize grip never opens editor after drag");
  File.WriteAllText(output,JsonSerializer.Serialize(new{passed=true,checks}));main.Close();
 }
 static IEnumerable<Control> Children(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in Children(x))yield return y;}}
}