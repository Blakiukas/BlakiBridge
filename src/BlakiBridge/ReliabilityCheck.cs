using System.Text.Json;
namespace BlakiBridge;
internal static class ReliabilityCheck
{
    internal static void Run(string output)
    {
        var checks=new List<string>();void Check(bool condition,string name){if(!condition)throw new InvalidOperationException(name);checks.Add(name);}
        var auto=new AutomaticDiagnostics();Check(!auto.Active(0),"Detailed diagnostics off by default");Check(auto.Trigger(10),"Automatic fault trigger");
        Check(!auto.Trigger(20),"Repeated faults do not extend window");Check(auto.Active(14999),"Bounded observation enabled");Check(!auto.Active(15010),"Automatic time expiry");
        Check(!auto.Trigger(16000),"Cooldown prevents repeated overhead");Check(auto.Trigger(300010),"Later fault can reopen window");
        for(int i=0;i<1024;i++)Check(auto.Take(300011),"Bounded sample "+i);
        Check(!auto.Take(300011),"Sample budget exhausted");
        var displays=MonitorModel.Detect();var baseline=displays.ToArray();
        MonitorModel Replace(MonitorModel m)=>new(){DeviceName=m.DeviceName,DeviceIdentity="replacement-"+m.DeviceIdentity,Bounds=m.Bounds,WorkingArea=m.WorkingArea,Primary=m.Primary,WidthMm=m.WidthMm,HeightMm=m.HeightMm,DpiX=m.DpiX,DpiY=m.DpiY,ModelName=m.ModelName};
        var changed=baseline.Select((m,i)=>i==0?Replace(m):m).ToArray();
        var recovery=new DisplayRecovery();recovery.SetBaseline(baseline);recovery.Begin(false);
        Check(!recovery.Observe(changed)&&!recovery.Observe(changed),"Automatic recovery rejects different identity");
        recovery.BeginReview();Check(!recovery.Observe(changed),"Manual review requires stable second snapshot");Check(recovery.Observe(changed)&&recovery.AcceptedDifferentSetup,"Manual review accepts stable new setup");
        var config=new BridgeConfig{Language="LT",OptimizeMouseSpeed=true};
        using var main=new LiveMain(diagnosticConfig:config){Opacity=0,ShowInTaskbar=false};main.Show();Application.DoEvents();
        Check(!main.ResourceTimerRunning,"No resource sampling on diagram");main.ToggleAbout();Check(main.ResourceTimerRunning,"About sampling enabled");main.ToggleGuide();Check(!main.ResourceTimerRunning,"Guide stops resource sampling");
        main.DiagnosticSave=()=>throw new IOException("simulated write failure");main.DiagnosticSaveNow();Check(main.SavingFailed&&main.HasIssue,"Save error persists globally");
        void Capture(string name){Application.DoEvents();using var bmp=new Bitmap(main.Width,main.Height);UiSnapshot.Paint(main,bmp);bmp.Save(Path.Combine(Path.GetDirectoryName(output)!,name+".png"));}
        Capture("save-error-guide");main.EditDisplay(0);Capture("save-error-dimensions");Check(main.SavingFailed,"Navigation cannot erase save error");
        main.DiagnosticSave=()=>{};main.RetryIssue();Check(!main.SavingFailed&&!main.HasIssue,"Successful retry clears save error");
        main.DiagnosticEngineFault();Application.DoEvents();Check(main.HasIssue,"Engine failure reported");Check(!main.ActivityControl.Controls.OfType<Label>().Any(l=>l.Text=="Veikia"),"Stopped engine cannot display Active");Capture("engine-error");
        main.DiagnosticReviewScreens();main.ProcessRecoverySnapshot(changed);Check(main.RecoveryPending,"Review waits for stable screens");main.ProcessRecoverySnapshot(changed);
        Check(!main.RecoveryPending&&!config.BridgeEnabled&&config.TopologyReviewRequired,"New setup stays disabled pending calibration review");
        Check(main.CanvasControl.Enabled,"Reviewed layout can be edited without restart");
        Check(!main.HasIssue,"Reviewed setup clears stale engine failure");
        int starts=main.DiagnosticEngineStarts;
        main.DiagnosticReviewScreens();main.ProcessRecoverySnapshot(changed);main.ProcessRecoverySnapshot(changed);
        Check(!config.BridgeEnabled&&main.DiagnosticEngineStarts==starts,"Review preserves disabled engine preference");
        main.Size=main.MinimumSize;
        foreach(var language in new[]{"EN","LT","RU"})
        {
            main.SelectLanguage(language);Check(main.uninstall.Text==UiText.Pick("Uninstall","Pašalinti programą","Удалить программу"),"Uninstall follows language "+language);Check(new BridgeConfig().Language=="EN","Fresh config defaults EN");if(!main.AboutPage.Visible)main.ToggleAbout();
            var contactBefore=main.copyEmail.Parent!.PointToScreen(Point.Empty);
            if(!main.resourceExplanation.Visible)main.resourceInfo.PerformClick();Application.DoEvents();
            var info=main.resourceExplanation;
            Capture("contact-fit-"+language);Check(main.copyEmail.Parent!.PointToScreen(Point.Empty)==contactBefore,"Explanation does not reflow contact "+language);
            Check(info.Visible&&main.AboutPage.ClientRectangle.Contains(info.Bounds),"Explanation contained "+language);
            var required=System.Windows.Forms.TextRenderer.MeasureText(info.Text,info.Font,new Size(info.Width-info.Padding.Horizontal,int.MaxValue),TextFormatFlags.WordBreak);
            Check(required.Height<=info.Height-info.Padding.Vertical,"Explanation text fits "+language);
            Capture("explanation-"+language);typeof(Control).GetMethod("OnMouseDown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(main.AboutPage,[new MouseEventArgs(MouseButtons.Left,1,1,1,0)]);Check(!info.Visible,"Blank click closes explanation "+language);Check(main.copyEmail.Cursor==Cursors.Hand,"Copy pointing cursor "+language);foreach(var label in main.AboutPage.Controls.OfType<Label>().Where(c=>c!=info&&c.Visible)){main.resourceInfo.PerformClick();typeof(Control).GetMethod("OnMouseDown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(label,[new MouseEventArgs(MouseButtons.Left,1,1,1,0)]);Check(!info.Visible,"Label background closes explanation "+language);}
        }
        main.Hide();Check(!main.ResourceTimerRunning,"No resource timer while hidden");main.Close();
        foreach(var state in new[]{("active",Color.FromArgb(126,220,42)),("off",Color.Gray),("attention",AppTheme.Accent)})
        {using var icon=LiveMain.CreateStateIcon(state.Item2);using var bitmap=icon.ToBitmap();bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!,"tray-"+state.Item1+".png"));}
        File.WriteAllText(output,JsonSerializer.Serialize(new{passed=true,checks=checks.Count,results=checks}));
    }
}
