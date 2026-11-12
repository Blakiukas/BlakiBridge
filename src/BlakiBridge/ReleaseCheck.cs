using System.Reflection;
using System.Text.Json;
namespace BlakiBridge;
internal static class ReleaseCheck
{
    internal static void Run(string output)
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);checks.Add(name);}
        using var main=new LiveMain(diagnosticConfig:new BridgeConfig{WelcomeGuideShown=true,MouseControlAdviceShown=true});
        main.Show();Application.DoEvents();
        var canvas=main.CanvasControl;
        void Mouse(string name,Point point)=>typeof(MonitorCanvas).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(canvas,[new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)]);
        using(var image=new Bitmap(main.Width,main.Height))UiSnapshot.Paint(main,image);
        int index=canvas.Monitors.Count-1;
        var rect=canvas.LabelMeasurements[index].Bounds;
        var center=new Point(rect.Left+rect.Width/2,rect.Top+rect.Height/2);
        int Selected()=> (int)typeof(LiveMain).GetField("selectedDisplay",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(main)!;
        var original=JsonSerializer.Serialize(canvas.DiagramPositions);
        Mouse("OnMouseDown",center);
        Check(canvas.SelectedIndex==index && Selected()==index,"Canvas and settings select on mouse down");
        Mouse("OnMouseMove",new Point(center.X+35,center.Y+20));
        Check(canvas.SelectedIndex==index && Selected()==index,"Selection stays synchronized during drag");
        using(var image=new Bitmap(main.Width,main.Height)){UiSnapshot.Paint(main,image);image.Save(Path.ChangeExtension(output,"png"));}
        canvas.CancelInteraction();
        Check(original==JsonSerializer.Serialize(canvas.DiagramPositions),"Cancel restores diagram");
        // A move onto another monitor must restore geometry and raise feedback.
        if(canvas.Monitors.Count>1)
        {
            using(var image=new Bitmap(main.Width,main.Height))UiSnapshot.Paint(main,image);
            rect=canvas.LabelMeasurements[index].Bounds;
            center=new Point(rect.Left+rect.Width/2,rect.Top+rect.Height/2);
            var other=canvas.LabelMeasurements[index==0?1:0].Bounds;
            bool rejected=false;canvas.MoveRejected+=()=>rejected=true;
            Mouse("OnMouseDown",center);
            var end=new Point(other.Left+other.Width/2,other.Top+other.Height/2);
            Mouse("OnMouseMove",end);Mouse("OnMouseUp",end);
            Check(rejected,"Overlap emits feedback");
            Check(original==JsonSerializer.Serialize(canvas.DiagramPositions),"Rejected move restores diagram");
        }
        if(AppStorage.IsPortable)Check(BridgeConfig.FilePath==Path.Combine(AppContext.BaseDirectory,"Data","config.json"),"Portable settings path");
        var path=Path.ChangeExtension(output,"config.json");
        new BridgeConfig{Language="LT",BridgeEnabled=true}.Save(path);
        Check(BridgeConfig.Load(path).Language=="LT" && BridgeConfig.Load(path).BridgeEnabled,"Settings round-trip");
        main.Close();
        File.WriteAllText(output,JsonSerializer.Serialize(new{passed=true,checks,portable=AppStorage.IsPortable,version=typeof(ReleaseCheck).Assembly.GetName().Version?.ToString()}));
    }
}
