using System.Runtime.InteropServices;
namespace BlakiBridge;
public sealed partial class LiveMain
{
    private readonly Panel issueBand=new(){Dock=DockStyle.Top,Height=56,Visible=false,BackColor=AppTheme.Surface,Padding=new Padding(22,6,22,6)};
    private readonly SmoothLabel issueText=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=AppTheme.Text,Padding=new Padding(0,0,12,0)};
    private readonly RoundedButton reviewAction=new(){Dock=DockStyle.Right,Width=150,Visible=false};
    private readonly RoundedButton issueAction=new(){Dock=DockStyle.Right,Width=164};
    private string? saveFailure,engineFailure;
    private Action? retryIssue;
    private Icon[]? stateIcons;
    private int lastTrayState=-1;
    internal Action? DiagnosticSave;
    internal bool HasIssue=>saveFailure is not null || engineFailure is not null || recovery.Pending;
    internal bool SavingFailed=>saveFailure is not null;
    internal void RetryIssue()=>retryIssue?.Invoke();
    internal void DiagnosticSaveNow()=>Save();
    internal void DiagnosticReviewScreens(){if(!diagnostics)throw new InvalidOperationException();ReviewScreens();}
    internal void DiagnosticEngineFault()
    {
        if(!diagnostics)throw new InvalidOperationException();
        bridge=new MouseBridge(config.Transitions,monitors,config);bridge.Faulted+=EngineFailed;bridge.Fail(new InvalidOperationException("diagnostic engine failure"));
    }
    private void InitializeIssueUi()
    {
        issueBand.Controls.AddRange([issueText,reviewAction,issueAction]);reviewAction.Click+=(_,_)=>ReviewScreens();issueAction.Click+=(_,_)=>retryIssue?.Invoke();
        Controls.Add(issueBand);Controls.SetChildIndex(issueBand,1);
        Disposed+=(_,_)=>{if(stateIcons!=null)foreach(var icon in stateIcons)icon.Dispose();};
    }
    private void UpdateIssue()
    {
        string? message=saveFailure??engineFailure;
        string action=UiText.Pick("Retry","Bandyti dar kartą","Повторить");
        retryIssue=saveFailure!=null?Save:engineFailure!=null?()=>{enabled.Checked=true;if(!enabled.Checked)ApplyEngine();}:null;
        if(message is null && recovery.Pending)
        {
            message=UiText.Pick("Screen configuration changed. Correction is paused. Check the connected screens.","Pasikeitė ekranų konfigūracija. Korekcija pristabdyta. Patikrinkite prijungtus ekranus.","Конфигурация экранов изменилась. Коррекция приостановлена. Проверьте экраны.");
            message=recovery.Reason switch {
                "missing"=>UiText.Pick("Waiting for a disconnected monitor. Correction is paused.","Laukiama atjungto monitoriaus. Korekcija pristabdyta.","Ожидание отключённого монитора. Коррекция приостановлена."),
                "identity"=>UiText.Pick("Monitor identity cannot be matched safely. Check screens.","Nepavyksta patikimai atpažinti monitoriaus. Patikrinkite ekranus.","Не удалось надёжно определить монитор. Проверьте экраны."),
                "geometry"=>UiText.Pick("Screen position or resolution changed. Check screens.","Pasikeitė ekrano vieta arba raiška. Patikrinkite ekranus.","Изменились положение или разрешение экрана. Проверьте экраны."),
                _=>message};
            retryIssue=()=>BeginRecovery(false);
        }
        reviewAction.Visible=recovery.Pending;reviewAction.Text=UiText.Pick("Check screens","Patikrinti ekranus","Проверить экраны");
        issueText.Text=message??"";issueAction.Text=action;issueBand.Visible=message!=null;
        issueBand.Height=(int)(56*DeviceDpi/96f);RestoreLogicalMinimum();
        if(message!=null && Height<MinimumSize.Height)Height=MinimumSize.Height;
    }
    private void AnnounceIssue(string message)
    {if(!diagnostics)tray.ShowBalloonTip(5000,"BlakiBridge",message,ToolTipIcon.Warning);}
    private void EngineFailed(MouseBridge sender,Exception error)
    {
        if(IsDisposed || !IsHandleCreated)return;
        try{BeginInvoke((Action)(()=>
        {
            if(bridge!=sender || closing)return;
            StopEngine();updating=true;enabled.Checked=false;updating=false;
            engineFailure=UiText.Pick("Mouse correction stopped after an error. You can retry enabling it.","Dėl klaidos pelės korekcija sustabdyta. Galite bandyti ją įjungti iš naujo.","Коррекция мыши остановлена из-за ошибки. Можно повторить включение.");
            UpdateStatus();AnnounceIssue(engineFailure);
        }));}catch(InvalidOperationException){}
    }
    private void ReviewScreens()
    {
        engineFailure=null;BeginRecovery(false);recovery.BeginReview();
        if(!diagnostics || DiagnosticUseRecoveryTimer)recoveryTimer.Start();
        ShowPage(screensPage);UpdateStatus();
    }
    private void AcceptReviewedScreens(IReadOnlyList<MonitorModel> current)
    {
        // Preserve only settings whose display name AND hardware identity are unchanged.
        var keep=current.Where(m=>monitors.Any(old=>old.DeviceName==m.DeviceName && old.DeviceIdentity==m.DeviceIdentity)).Select(m=>m.DeviceName).ToHashSet();
        foreach(var name in config.Measurements.Keys.Where(k=>!keep.Contains(k)).ToArray())config.Measurements.Remove(name);
        foreach(var name in config.MouseSpeedPercent.Keys.Where(k=>!keep.Contains(k)).ToArray())config.MouseSpeedPercent.Remove(name);
        foreach(var name in canvas.Measurements.Keys.Where(k=>!keep.Contains(k)).ToArray())canvas.Measurements.Remove(name);
        foreach(var name in canvas.ManualWidths.Keys.Where(k=>!keep.Contains(k)).ToArray())canvas.ManualWidths.Remove(name);
        foreach(var name in canvas.DiagramPositions.Keys.Where(k=>!keep.Contains(k)).ToArray())canvas.DiagramPositions.Remove(name);
        config.Transitions=config.Transitions.Where(t=>keep.Contains(t.A)&&keep.Contains(t.B)).ToList();
        config.Connections=config.Connections?.Where(t=>keep.Contains(t.A)&&keep.Contains(t.B)).ToList();
        monitors=current;canvas.Monitors=current;
        foreach(var name in canvas.Positions.Keys.Where(k=>!keep.Contains(k)).ToArray())canvas.Positions.Remove(name);
        foreach(var m in current)if(!canvas.Positions.ContainsKey(m.DeviceName))canvas.Positions[m.DeviceName]=m.Bounds.Location;
        var suggestions=MonitorTopology.Suggest(canvas);
        if(config.Connections==null)config.Connections=suggestions;
        else config.Connections.AddRange(suggestions.Where(l=>(!keep.Contains(l.A)||!keep.Contains(l.B))&&!config.Connections.Any(old=>GapSettings.Key(old.A,old.B)==GapSettings.Key(l.A,l.B))));
        config.Gaps=config.Gaps.Where(g=>config.Connections.Any(l=>keep.Contains(l.A)&&keep.Contains(l.B)&&GapSettings.Key(l.A,l.B)==g.Key)).ToDictionary(g=>g.Key,g=>g.Value);
        RebuildMaps();config.TopologyReviewRequired=true;config.BridgeEnabled=false;
        updating=true;enabled.Checked=false;updating=false;
        selectedDisplay=-1;screenNavigation.SetMonitor("");foreach(var tab in screenNavigation.Tabs)tab.Enabled=false;
        recovery.SetBaseline(current);Save();
    }
    private void UpdateTrayState(bool active,bool waiting)
    {
        int state=saveFailure!=null || engineFailure!=null || waiting?2:active?0:1;
        if(stateIcons is null)
        {
            stateIcons=new[]{Color.FromArgb(126,220,42),Color.Gray,AppTheme.Accent}.Select(CreateStateIcon).ToArray();
        }
        if(state!=lastTrayState){tray.Icon=stateIcons[state];lastTrayState=state;}
        tray.Text="BlakiBridge · "+(state==2?UiText.Pick("Needs attention","Reikia dėmesio","Требует внимания"):state==0?UiText.Pick("Enabled","Įjungta","Включено"):UiText.Pick("Off","Išjungta","Выключено"));
    }
    internal static Icon CreateStateIcon(Color color)
    {
        using var image=new Bitmap(32,32);using(var g=Graphics.FromImage(image))
        {g.Clear(Color.Transparent);BrandGlyph.Draw(g,new RectangleF(0,0,32,32));g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using var ring=new SolidBrush(AppTheme.Background);using var fill=new SolidBrush(color);g.FillEllipse(ring,19,0,13,13);g.FillEllipse(fill,21,2,9,9);}
        IntPtr handle=image.GetHicon();try{using var source=Icon.FromHandle(handle);return (Icon)source.Clone();}finally{DestroyIcon(handle);}
    }
    [DllImport("user32.dll")]private static extern bool DestroyIcon(IntPtr handle);
}
