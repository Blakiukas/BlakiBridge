using System.Runtime.InteropServices;

namespace BlakiBridge;

public sealed partial class LiveMain : ThemedForm
{
    private readonly System.Windows.Forms.Timer resizeLayoutTimer=new(){Interval=16};
    private bool deferredResize;
    protected override void OnResizeBegin(EventArgs e)
    {
        base.OnResizeBegin(e);
        if(deferredResize)return;
        deferredResize=true;SuspendLayout();resizeLayoutTimer.Start();
    }
    protected override void OnResizeEnd(EventArgs e)
    {
        resizeLayoutTimer.Stop();
        if(deferredResize){deferredResize=false;ResumeLayout(true);}
        base.OnResizeEnd(e);Invalidate(true);
    }
    private void FlushResizeLayout()
    {
        if(!deferredResize)return;
        ResumeLayout(true);SuspendLayout();
    }
    private void RestoreLogicalMinimum()
    {
        if(IsDisposed || Disposing)return;
        // Native resize styles must not subtract their invisible frame from our client-only minimum.
        MinimumSize=new Size((int)Math.Round(960*DeviceDpi/96d),(int)Math.Round((660+(HasIssue?56:0))*DeviceDpi/96d));
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);RestoreLogicalMinimum();
    }
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if(IsHandleCreated)BeginInvoke((Action)RestoreLogicalMinimum);
    }
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.Style = (cp.Style & ~0x00C00000) | 0x000F0000; return cp; }
    }
    private readonly BridgeConfig config;
    private readonly bool diagnostics;
    private readonly MonitorCanvas canvas = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel optionBand = new LayoutRow() { Dock = DockStyle.Top, Padding = new Padding(22,8,12,8), BackColor = AppTheme.Surface };
    private readonly FlowLayoutPanel footerBand = new LayoutRow() { Dock = DockStyle.Bottom, Padding = new Padding(22,8,22,8), BackColor = AppTheme.Surface };
    private readonly Panel footerSpacer = new() { Height = 1, Margin = Padding.Empty };
    private bool syncingBands;
    internal int OptionBandHeight => optionBand.Height;
    internal int FooterBandHeight => footerBand.Height;
    private static readonly string AppVersion = "v" + (typeof(LiveMain).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");
    private void FitBands()
    {
        if (syncingBands) return;
        syncingBands = true;
        try
        {
            int used = footerBand.Controls.Cast<Control>().Where(c => c != footerSpacer && c.Visible).Sum(c => (c.AutoSize ? c.GetPreferredSize(Size.Empty).Width : c.Width) + c.Margin.Horizontal);
            footerSpacer.Width = Math.Max(0,footerBand.ClientSize.Width-footerBand.Padding.Horizontal-used-1);
            int height = (int)(64*DeviceDpi/96f);
            optionBand.Height=height; footerBand.Height=height;
        }
        finally { syncingBands=false; }
    }
    internal readonly BackButton AboutBack = new();
    private readonly Panel workspace = new() { Dock = DockStyle.Fill };
    private readonly DisplayNavigation screenNavigation=new();
    private int selectedDisplay=-1;
    private readonly Panel screensPage = new() { Dock = DockStyle.Fill, Padding = new Padding(22,0,22,20) };
    private readonly Panel aboutPage = new() { Dock = DockStyle.Fill, Padding = new Padding(22,12,22,12), AutoScroll = false, Visible = false };
    private readonly ResourceExplanationLabel aboutTitle=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(16,0,0,0),Font=new Font("Segoe UI",11,FontStyle.Bold)};
    private readonly ResourceExplanationLabel aboutEmergency=new(){Dock=DockStyle.Right,Width=420,TextAlign=ContentAlignment.MiddleRight};
    private readonly SmoothLabel aboutVersion=new(){Dock=DockStyle.Bottom,Height=30,TextAlign=ContentAlignment.MiddleRight};
    internal readonly SmoothLabel contactText=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0)};
    internal readonly RoundedButton copyEmail=new(){AutoSize=true,Text="Kopijuoti",Margin=new Padding(10,0,0,0)};
    private readonly System.Windows.Forms.Timer copyNotice=new(){Interval=1800};
    private readonly SmoothLabel aboutText = new() { Dock = DockStyle.Top, Height = 180, AutoSize = false };
    private readonly RoundedButton aboutButton = new() { AutoSize = true, FlatStyle = FlatStyle.Flat, Padding = new Padding(10,5,10,5), BackColor = AppTheme.Background };
    private readonly StatusDot activityDot = new() { Size=new Size(14,14), Margin=new Padding(0,0,8,0) };
    private readonly SmoothLabel activityText = new() { AutoSize = true,Font=new Font("Segoe UI",11,FontStyle.Bold), ForeColor = Color.WhiteSmoke, Margin = Padding.Empty };
    private readonly FlowLayoutPanel footerActions = new LayoutRow() { AutoSize = true, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty, Font = new Font("Segoe UI",11) };
    private readonly FlowLayoutPanel activity = new LayoutRow() { AutoSize = true, WrapContents = false, Padding = new Padding(8,0,8,0), Font = new Font("Segoe UI",11) };
    internal MonitorCanvas CanvasControl => canvas;
    internal UserGuide GuideControl => guide;
    internal Panel ContentArea => workspace;
    internal Panel ScreensPage => screensPage;
    internal Panel AboutPage => aboutPage;
    internal Control FooterActions => footerActions;
    internal Control ActivityControl => activity;
    private readonly UserGuide guide = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly RoundedButton help = new() { Text = "How to use", AutoSize = true, FlatStyle = FlatStyle.Flat, Padding = new Padding(10, 5, 10, 5), BackColor = AppTheme.Background };
    internal readonly RoundedButton uninstall=new(){AutoSize=true,Cursor=Cursors.Hand};
    private readonly LanguageBox language = new();
    internal void SelectLanguage(string value) => language.SelectedItem = value;
    internal LanguageBox LanguageControl => language;
    internal void ToggleGuide() => ShowPage(guide.Visible ? screensPage : guide);
    internal void ToggleAbout() => ShowPage(aboutPage.Visible ? screensPage : aboutPage);

    private void ShowPage(Control page)
    {
        if (displayEditor is { } editor && page != editor) editor.Close();
        if (gapEditor is { } gap && page != gap) gap.Close();
        workspace.SuspendLayout();
        foreach (Control view in workspace.Controls) view.Visible = view == page;
        page.BringToFront();
        if (page is ScrollableControl scroll) scroll.AutoScrollPosition = Point.Empty;
        workspace.ResumeLayout(true);
        help.IsSelected=page==guide;help.Invalidate();
        aboutButton.IsSelected=page==aboutPage;aboutButton.Invalidate();
        FitBands(); UpdateResourceMeter();
    }
    private readonly CheckBox enabled = new BridgeSwitch() { Text = "Enabled", AutoSize = true };
    private readonly CheckBox adjust = new AdjustButton() { Text = "Adjust", AutoSize = true };
    private readonly CheckBox startup = new BridgeSwitch() { TrailingTrack = true, WindowsMark = true, Text = "Start with Windows", AutoSize = true };
    private readonly CheckBox optimize = new BridgeSwitch() { Text = "Optimize mouse speed", AutoSize = true };
    private readonly CheckBox useGap = new BridgeSwitch() { Text = "Use physical gap", AutoSize = true };
    internal Func<Transition, string, string?, decimal?>? DiagnosticGapEditor;
    internal void SelectGapMode(bool value) => useGap.Checked = value;
    private readonly System.Windows.Forms.Timer notice = new() { Interval = 3500 };
    private readonly System.Windows.Forms.Timer resourceTimer = new() { Interval = 2000 };
    private readonly ResourceMeter resourceMeter = new();
    private readonly SmoothLabel resources = new() { Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.MiddleLeft, Padding = Padding.Empty, Text = "CPU: —    RAM: —    Background CPU: —" };
    
    internal readonly DetailIconButton resourceInfo=new(){Cursor=Cursors.Hand};
    private readonly DetailIconButton updateCheck=new(){RefreshIcon=true,Enabled=false,Visible=false,TabStop=false};
    internal readonly ResourceExplanationLabel resourceExplanation=new(){HighlightResources=true,Dock=DockStyle.None,Height=150,Padding=new Padding(14),BackColor=AppTheme.Surface,Visible=false};
    private bool resourcesActive, backgroundSample;
    private double? backgroundCpu;
    internal bool ResourceTimerRunning => resourceTimer.Enabled;
    internal Label ResourceLabel => resources;
    private void UpdateResourceMeter()
    {
        if (closing) return;
        bool active = Visible && WindowState != FormWindowState.Minimized;
        try
        {
            var sample = resourceMeter.Sample();
            if (active && !resourcesActive && backgroundSample) backgroundCpu = sample.Cpu;
            if (!active && resourcesActive) backgroundSample = true;
            string cpu = active && resourcesActive && sample.Cpu is double value ? $"{value:0.00}%" : "—";
            resources.Text = $"CPU: {cpu}   RAM: {sample.Ram:0.0} MB   {UiText.Pick("Avg. background CPU","Vid. CPU fone","Сред. CPU в фоне")}: {(backgroundCpu is double bg ? $"{bg:0.00}%" : "—")}";
            resourcesActive = active; resourceTimer.Enabled = active && aboutPage.Visible;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        { resources.Text = UiText.T("Resource readings unavailable"); resourceTimer.Stop(); }
    }
    private bool escapeKey;
    private IntPtr powerNotification;
    internal bool PowerNotificationsRegistered => powerNotification != IntPtr.Zero;
    private readonly DisplayRecovery recovery = new();
    private readonly System.Windows.Forms.Timer recoveryTimer = new() { Interval = 1000 };
    internal bool DiagnosticUseRecoveryTimer;
    internal bool RecoveryPending => recovery.Pending;
    internal bool RecoveryTimerRunning => recoveryTimer.Enabled;
    internal int DiagnosticEngineStarts { get; private set; }
    private readonly NotifyIcon tray = new();
    private readonly bool startHidden;
    internal void ExitForSetup(){exitRequested=true;Close();}
    private bool exitRequested;
    private readonly SmoothLabel status = new() { Dock = DockStyle.Top, Height = 44, TextAlign = ContentAlignment.MiddleLeft, Padding = Padding.Empty, ForeColor = Color.Silver };
    private IReadOnlyList<MonitorModel> monitors = [];
    private DisplaySettingsForm? displayEditor;
    private PairGapForm? gapEditor;
    internal PairGapForm? GapEditor => gapEditor;
    private LiveMarkers? markers;
    private MouseBridge? bridge;
    private bool hotkey, updating, dragging, closing, initialized;
    public LiveMain(bool startHidden = false, BridgeConfig? diagnosticConfig = null)
    {
        resizeLayoutTimer.Tick+=(_,_)=>FlushResizeLayout();
        Disposed+=(_,_)=>resizeLayoutTimer.Dispose();
        diagnostics = diagnosticConfig is not null;
        config = diagnosticConfig ?? BridgeConfig.Load();
        config.Language = config.Language is "LT" or "RU" ? config.Language : "EN";
        UiText.Language = config.Language;
        uninstall.Text=UiText.Pick("Uninstall","Pašalinti programą","Удалить программу");
        exitRequested = diagnostics;
        this.startHidden = startHidden;
        FormBorderStyle = FormBorderStyle.None; Text = "BlakiBridge"; Size = new Size(960, 660); MinimumSize = new Size(960, 660);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 11);
        BackColor = AppTheme.Background; ForeColor = Color.White;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { if(resourceExplanation.Visible){resourceExplanation.Visible=false;resourceInfo.IsSelected=false;resourceInfo.Invalidate();}else adjust.Checked = false; e.Handled = true; } };
        var header = new WindowHeader(this, language) { Dock = DockStyle.Top, Height = 64 };
        language.SelectedItem = config.Language;
        help.FlatAppearance.BorderColor = Color.FromArgb(73,73,73);
        help.FlatAppearance.MouseOverBackColor = Color.FromArgb(50,50,50);
        help.Tag="book";help.Image = CreateBookIcon(); help.ImageAlign = ContentAlignment.MiddleLeft; help.TextImageRelation = TextImageRelation.ImageBeforeText;
        language.Margin = Padding.Empty;
        var controls = optionBand;
        foreach (var checkbox in new[] { enabled, adjust, optimize, startup, useGap }) checkbox.Margin = new Padding(0, 0, 20, 0);
        var settingsPanel = footerBand;
        var actions = footerActions;
        var support = new RoundedButton { Text = "Buy me a coffee", AutoSize = true, FlatStyle = FlatStyle.Flat, Padding = new Padding(6, 5, 6, 5), BackColor = AppTheme.Background, ForeColor = Color.WhiteSmoke };
        support.Tag="coffee";support.Image=new Bitmap(22,22);support.Disposed+=(_,_)=>support.Image?.Dispose();
        support.FlatAppearance.BorderColor = Color.FromArgb(73,73,73);
        support.Click += (_, _) => OpenSupportPage();
        aboutButton.Text = UiText.Pick("About", "Apie programą", "О программе");
        aboutButton.AccessibleName = "About BlakiBridge";
        aboutButton.FlatAppearance.BorderColor = Color.FromArgb(73,73,73);
        aboutButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50,50,50);
        aboutButton.MinimumSize = new Size(0, help.GetPreferredSize(Size.Empty).Height);
        aboutButton.Click += (_, _) => ToggleAbout();
        help.Margin=new Padding(16,3,3,3);
        actions.Controls.AddRange([startup, help, aboutButton]);
        var supportRow = new Panel { Dock = DockStyle.Fill,Padding=Padding.Empty };support.Margin=Padding.Empty;supportRow.Controls.Add(support);supportRow.Layout+=(_,_)=>{var size=support.GetPreferredSize(Size.Empty);support.SetBounds(supportRow.ClientSize.Width-size.Width,(supportRow.ClientSize.Height-size.Height)/2,size.Width,size.Height);};
        var aboutBottom=new Panel{Dock=DockStyle.Bottom,Height=84};
        var aboutSupport=new Panel{Dock=DockStyle.Right,Width=240};
        var versionRow=new Panel{Dock=DockStyle.Bottom,Height=34};
        updateCheck.Dock=DockStyle.Right;updateCheck.Width=32;aboutVersion.Dock=DockStyle.Fill;aboutVersion.Padding=new Padding(0,0,10,0);versionRow.Controls.AddRange([aboutVersion,updateCheck]);
        versionRow.Dock=DockStyle.Right;versionRow.Width=240;
        resources.Dock=DockStyle.Fill;resources.TextAlign=ContentAlignment.BottomLeft;resources.Padding=new Padding(0,0,0,6);
        var resourceRow=new Panel{Dock=DockStyle.Fill};
        resources.Dock=DockStyle.Left;resources.TextAlign=ContentAlignment.MiddleLeft;resources.Padding=Padding.Empty;
        resourceRow.Controls.AddRange([resources,resourceInfo]);
        void FitResourceRow(){float scale=DeviceDpi/96f;resources.Width=Math.Min(Math.Max(0,resourceRow.Width-(int)(38*scale)),AppText.MeasureText(resources.Text,resources.Font).Width+(int)(8*scale));resourceInfo.SetBounds(resources.Right,(resourceRow.Height-(int)(32*scale))/2,(int)(32*scale),(int)(32*scale));}
        resourceRow.Layout+=(_,_)=>FitResourceRow();resources.TextChanged+=(_,_)=>FitResourceRow();
        resourceInfo.Click+=(_,_)=>{resourceExplanation.Visible=!resourceExplanation.Visible;if(resourceExplanation.Visible)resourceExplanation.BringToFront();resourceInfo.IsSelected=resourceExplanation.Visible;resourceInfo.Invalidate();};
        var resourceArea=new Panel{Dock=DockStyle.Fill};resourceArea.Controls.Add(resourceRow);
        var readingsFrame=new RoundedSurface{Dock=DockStyle.Bottom,Height=40,Padding=new Padding(8,2,8,2)};
        readingsFrame.Controls.AddRange([resourceArea,versionRow]);supportRow.Dock=DockStyle.Fill;
        var supportArea=new Panel{Dock=DockStyle.Fill};supportRow.Dock=DockStyle.Right;supportRow.Width=210;
        var copyright=new SmoothLabel{Text="© 2026 Blakiukas",CenterCapHeight=true,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
        uninstall.Visible=InstalledApp.CanUninstall;
        uninstall.Click+=(_,_)=>InstalledApp.Uninstall();
        copyright.Dock=DockStyle.Left;copyright.Width=160;uninstall.Dock=DockStyle.Right;
        var uninstallHost=new Panel{Dock=DockStyle.Right,Width=180,Visible=InstalledApp.CanUninstall};uninstallHost.Controls.Add(uninstall);
        uninstallHost.Layout+=(_,_)=>{var z=uninstall.GetPreferredSize(Size.Empty);uninstall.SetBounds(uninstallHost.Width-z.Width,(uninstallHost.Height-z.Height)/2,z.Width,z.Height);};uninstall.Dock=DockStyle.None;
        supportArea.Controls.AddRange([copyright,uninstallHost,supportRow]);aboutBottom.Controls.AddRange([supportArea,readingsFrame]);
        activityText.CenterCapHeight=true;activity.Controls.AddRange([activityDot, activityText]);
        var aboutNavigation = new Panel { Dock = DockStyle.Top, Height = 44 };
        AboutBack.Dock=DockStyle.Left;aboutEmergency.Padding=new Padding(10,0,10,0);
        var titleHost=new Panel{Dock=DockStyle.Fill,Padding=new Padding(8,0,8,0)};titleHost.Controls.Add(aboutTitle);
        aboutNavigation.Controls.AddRange([titleHost,aboutEmergency,AboutBack]);AboutBack.Click += (_, _) => ShowPage(screensPage);
        guide.Back.Click += (_, _) => ShowPage(screensPage);
        aboutText.Padding=new Padding(0,32,0,0);
        var contact=new DetailIconButton{EmailIcon=true,Cursor=Cursors.Hand,Dock=DockStyle.Left,Width=32,AccessibleName="Email blaki.support@gmail.com"};
        contact.Click+=(_,_)=>{try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("mailto:blaki.support@gmail.com"){UseShellExecute=true});}catch(Exception ex){if(!diagnostics)BridgeTrace.Write("contact_open_failed",new{error=ex.Message});}};
        var contactRow=new Panel{Dock=DockStyle.Top,Height=28};contactRow.Controls.AddRange([contactText,contact]);
        var contactCard=new RoundedSurface{Dock=DockStyle.Top,Height=82,Padding=new Padding(12,8,12,8)};
        var contactHost=new Panel{Dock=DockStyle.Bottom,Height=106,Padding=new Padding(0,0,0,24)};
        contactHost.Controls.Add(contactCard);
        void PositionExplanation(){if(!resourceExplanation.Visible)return;int gap=(int)(4*DeviceDpi/96f);var anchor=aboutPage.PointToClient(aboutBottom.PointToScreen(Point.Empty));resourceExplanation.SetBounds(aboutPage.Padding.Left,anchor.Y-gap-resourceExplanation.Height,aboutPage.ClientSize.Width-aboutPage.Padding.Horizontal,resourceExplanation.Height);resourceExplanation.BringToFront();}
        resourceExplanation.VisibleChanged+=(_,_)=>{contactHost.Visible=!resourceExplanation.Visible;PositionExplanation();};aboutPage.SizeChanged+=(_,_)=>PositionExplanation();

        void FitAboutCopy()
        {
            int width=Math.Max(80,aboutText.Width-aboutText.Padding.Horizontal);
            int height=TextRenderer.MeasureText(aboutText.Text,aboutText.Font,new Size(width,int.MaxValue),TextFormatFlags.WordBreak).Height+aboutText.Padding.Vertical;
            if(aboutText.Height!=height)aboutText.Height=height;
        }
        aboutText.SizeChanged+=(_,_)=>FitAboutCopy();aboutText.TextChanged+=(_,_)=>FitAboutCopy();aboutText.FontChanged+=(_,_)=>FitAboutCopy();
        var addressRow=new LayoutRow{Dock=DockStyle.Top,Height=34,Padding=new Padding(40,0,0,0),WrapContents=false};
        addressRow.Controls.AddRange([new SmoothLabel{Text="blaki.support@gmail.com",AutoSize=true,Margin=Padding.Empty},copyEmail]);
        copyNotice.Tick+=(_,_)=>{copyNotice.Stop();copyEmail.Text=UiText.Pick("Copy","Kopijuoti","Копировать");};
        copyEmail.Click+=(_,_)=>{try{Clipboard.SetText("blaki.support@gmail.com");copyEmail.Text=UiText.Pick("Copied","Nukopijuota","Скопировано");copyNotice.Start();}catch(System.Runtime.InteropServices.ExternalException){copyEmail.Text=UiText.Pick("Retry","Pakartoti","Повторить");}};
        Disposed+=(_,_)=>copyNotice.Dispose();
        contactCard.Controls.AddRange([addressRow,contactRow]);
        aboutPage.Controls.AddRange([contactHost,aboutText,resourceExplanation,aboutBottom,aboutNavigation]);
        resources.HighlightResources=true;
        resources.Font = new Font("Segoe UI", 11);
        void CloseExplanationOnBlank(object? sender,MouseEventArgs e){if(resourceExplanation.Visible&&e.Button==MouseButtons.Left){resourceExplanation.Visible=false;resourceInfo.IsSelected=false;resourceInfo.Invalidate();}}
        void WatchBlank(Control c){if(c==resourceInfo||c==resourceExplanation)return;c.MouseDown+=CloseExplanationOnBlank;c.ControlAdded+=(_,e)=>{if(e.Control is not null)WatchBlank(e.Control);};foreach(Control child in c.Controls)WatchBlank(child);}

        settingsPanel.Controls.AddRange([actions, footerSpacer, activity]);
        SizeChanged += (_, _) => { if(!deferredResize){FitBands();optionBand.Invalidate(true);} };
        settingsPanel.Layout += (_, _) => FitBands();
        ResizeEnd += (_, _) => Invalidate(true);
        controls.Layout += (_, _) => FitBands();
        help.Click += (_, _) => ToggleGuide();
        canvas.GapSelected += EditGap;
        useGap.CheckedChanged += (_, _) => { if (!updating) RequestGapMode(useGap.Checked); };
        controls.Controls.AddRange([enabled, adjust, optimize, useGap]);
#if STORE_PACKAGE
        ConfigurePackageStartup();
#else
        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            startup.Checked = !AppStorage.IsPortable && key?.GetValue("BlakiBridge") is string;
        startup.Enabled = !AppStorage.IsPortable;
        startup.CheckedChanged += (_, _) =>
        {
            if (updating || diagnostics || AppStorage.IsPortable) return;
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (startup.Checked) key.SetValue("BlakiBridge", "\"" + Application.ExecutablePath + "\" --startup");
                else key.DeleteValue("BlakiBridge", false);
            }
            catch (Exception ex) { updating = true; startup.Checked = !startup.Checked; updating = false; MessageBox.Show(this, ex.Message, "Startup setting could not be saved"); }
        };
#endif
        tray.Icon = Icon; tray.Text = "BlakiBridge"; tray.Visible = !diagnostics;
        var menu = new ContextMenuStrip { Renderer = new TrayMenuRenderer(), ShowImageMargin = false, ShowCheckMargin = true, BackColor = TrayMenuRenderer.Background, ForeColor = Color.FromArgb(238,238,238), Font = SystemFonts.MenuFont };
        menu.Items.Add("Open BlakiBridge", null, (_, _) => { Show(); WindowState = FormWindowState.Normal; Activate(); });
        AddTrayToggle(menu, "Enable program", enabled);
        AddTrayToggle(menu, "Optimize mouse speed", optimize);
        AddTrayToggle(menu, "Use physical gap", useGap);
        menu.Items.Add("Close program", null, (_, _) => { exitRequested = true; Close(); });
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
        var screenInfo=new InformationBand(status){Height=64,Padding=new Padding(0,0,0,12),JoinNavigation=true};
        screenInfo.Margin=Padding.Empty;
        screensPage.Controls.AddRange([canvas, screenInfo]);
        // The same tab row is present before and after choosing a monitor.
        var mainNavigationHost=new Panel{Dock=DockStyle.Top,Height=52,Margin=Padding.Empty};
        screenNavigation.Padding=new Padding(0,8,0,0);
        mainNavigationHost.Controls.Add(screenNavigation);screensPage.Controls.Add(mainNavigationHost);
        screenNavigation.Back.Down=true;screenNavigation.Back.RefreshLanguage();screenNavigation.Back.Enabled=false;
        for(int i=0;i<3;i++){int tab=i;screenNavigation.Tabs[i].Enabled=false;screenNavigation.Tabs[i].Click+=(_,_)=>{if(selectedDisplay>=0){EditDisplay(selectedDisplay);displayEditor?.SelectTab(tab);}};}
        workspace.Controls.AddRange([screensPage, guide, aboutPage]);
        Controls.AddRange([workspace, settingsPanel, controls, header]);InitializeIssueUi();
        ((LayoutRow)footerBand).ResizeGrip=true;WatchBlank(this);
        language.SelectedIndexChanged += (_, _) => { config.Language = (string)language.SelectedItem!; ApplyLanguage(); Save(); };
        ApplyLanguage();
        recoveryTimer.Tick += (_, _) => PollRecovery();
        resourceTimer.Tick += (_, _) => UpdateResourceMeter();
        VisibleChanged += (_, _) => { UpdateResourceMeter(); if (initialized && Visible) ShowWelcomeGuide(); };
        Resize += (_, _) => { if (resourcesActive != (Visible && WindowState != FormWindowState.Minimized)) UpdateResourceMeter(); };
        notice.Tick += (_, _) => { notice.Stop(); UpdateStatus(); };
        optimize.CheckedChanged += (_, _) =>
        {
            if (updating) return;
            config.OptimizeMouseSpeed = optimize.Checked; displayEditor?.SetSpeedEnabled(optimize.Checked); ApplyEngine(); Save();
        };
        canvas.DisplaySelected += SelectDisplay;
        canvas.MoveRejected += () =>
        {
            status.Text = UiText.Pick("Screens cannot overlap. The previous position was restored.", "Ekranai negali persidengti. Ankstesnė padėtis atkurta.", "Экраны не могут перекрываться. Прежнее положение восстановлено.");
            notice.Stop(); notice.Start();
        };
        enabled.CheckedChanged += (_, _) => { if (!updating) { if (!diagnostics) BridgeTrace.Write("enabled_requested", new { enabled = enabled.Checked }); config.BridgeEnabled = enabled.Checked; if(enabled.Checked)config.TopologyReviewRequired=false; if (recovery.Pending && enabled.Checked && !recovery.Suspended) BeginRecovery(false); ApplyEngine(); Save(); } };
        adjust.CheckedChanged += (_, _) => { if (!updating) { if (adjust.Checked && (!enabled.Checked || recovery.Pending)) { adjust.Checked = false; return; } ShowMarkers(); } };
        canvas.LayoutChanged += (_, _) => { RebuildMaps(); ShowMarkers(); ApplyEngine(); Save(); };
        canvas.AppearanceChanged += (_, _) => { Save(); ApplyEngine(); };
        Shown += (_, _) => Initialize();
        FormClosing += (_, e) =>
        {
            displayEditor?.Close(); gapEditor?.Close();
            if (!exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; adjust.Checked = false; Save(); Hide();
                tray.ShowBalloonTip(2500, UiText.T("BlakiBridge is still running"), UiText.Pick("Open it from the system tray. Use Close program to stop it completely.", "Atidarykite iš sistemos dėklo. Norėdami visiškai uždaryti, pasirinkite Uždaryti programą.", "Откройте из системного трея. Для полного выхода выберите Закрыть программу."), ToolTipIcon.Info);
                return;
            }
            closing = true; recoveryTimer.Stop(); markers?.Dispose(); markers = null; Save(); StopEngine(); if (escapeKey) UnregisterHotKey(Handle, 913); notice.Dispose(); resourceTimer.Dispose(); resourceMeter.Dispose();  tray.Dispose();
        };
    }
    internal ContextMenuStrip TrayMenu => tray.ContextMenuStrip!;
    private static void AddTrayToggle(ContextMenuStrip menu, string text, CheckBox control)
    {
        var item = new ToolStripMenuItem(text) { Checked = control.Checked };
        // Commit through the existing control: gap setup can cancel, and engine startup can fail.
        item.Click += (_, _) => { control.Checked = !control.Checked; item.Checked = control.Checked; };
        control.CheckedChanged += (_, _) => item.Checked = control.Checked;
        menu.Opening += (_, _) => item.Checked = control.Checked;
        menu.Items.Add(item);
    }
    private void OpenSupportPage()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://buymeacoffee.com/blakiukas") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, UiText.Pick("Could not open the browser. ", "Nepavyko atidaryti naršyklės. ", "Не удалось открыть браузер. ") + "https://buymeacoffee.com/blakiukas", "Buy me a coffee", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    private static Bitmap CreateBookIcon()
    {
        var image = new Bitmap(22, 22);
        using var g = Graphics.FromImage(image); g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.WhiteSmoke, 1.4f);
        g.DrawLines(pen, [new Point(11,5), new Point(6,3), new Point(2,4), new Point(2,17), new Point(6,16), new Point(11,18), new Point(16,16), new Point(20,17), new Point(20,4), new Point(16,3), new Point(11,5), new Point(11,18)]);
        return image;
    }
    private void ApplyLanguage()
    {
        displayEditor?.Close(); gapEditor?.Close();
        UiText.Language = config.Language;
        uninstall.Text=UiText.Pick("Uninstall","Pašalinti programą","Удалить программу");
        screenNavigation.RefreshText(); screenNavigation.Back.RefreshLanguage();
        guide.Back.RefreshLanguage(); AboutBack.RefreshLanguage();
        canvas.Invalidate();
        enabled.Text = UiText.T("Enabled"); ((AdjustButton)adjust).RefreshText();
        optimize.Text = UiText.T("Optimize mouse speed"); useGap.Text = UiText.T("Use physical gap"); startup.Text = UiText.T("Start with Windows");
        help.Text = UiText.T("How to use");
        guide.RefreshLanguage();
        aboutButton.Text = UiText.Pick("About", "Apie programą", "О программе");
        aboutButton.AccessibleName = UiText.Pick("About BlakiBridge", "Apie BlakiBridge", "О BlakiBridge");
        aboutTitle.Text=UiText.Pick("About BlakiBridge", "Apie BlakiBridge", "О BlakiBridge");
        aboutVersion.Text="BlakiBridge "+AppVersion;
        aboutEmergency.Text=UiText.Pick("If the pointer moves incorrectly: Ctrl + Alt + F12.","Jei pelė juda netinkamai: Ctrl + Alt + F12.","Если указатель движется неправильно: Ctrl + Alt + F12.");
        aboutText.Text=UiText.Pick("The program is designed to make smooth mouse pointer transitions between monitors of different heights and resolutions as simple and automatic as possible.\nI created BlakiBridge for my own needs because I use three different monitors and could not find a convenient alternative.\nThis is my first program, created using Codex.","Programa skirta maksimaliai supaprastinti ir automatizuoti sklandų pelės kursoriaus perėjimą tarp skirtingo aukščio ir raiškos monitorių.\nBlakiBridge sukūriau savo poreikiams, nes naudoju tris skirtingus monitorius ir neradau patogios alternatyvos.\nTai pirmoji mano programa, sukurta naudojant Codex.","Программа создана, чтобы максимально упростить и автоматизировать плавный переход курсора мыши между мониторами разной высоты и разрешения.\nЯ создал BlakiBridge для своих нужд: у меня три разных монитора, и я не нашёл удобной альтернативы.\nЭто моя первая программа, созданная с помощью Codex.");
        copyNotice.Stop();copyEmail.Text=UiText.Pick("Copy","Kopijuoti","Копировать");
        contactText.Text=UiText.Pick("Write to me if you have problems or suggestions. I will do my best to help.","Parašykite man, jei kilo problemų ar turite pasiūlymų. Pasistengsiu padėti.","Напишите мне, если возникли проблемы или есть предложения. Постараюсь помочь.");
        resourceInfo.AccessibleName=UiText.Pick("Explain resource readings","Paaiškinti resursų rodmenis","Пояснить показатели ресурсов");
        updateCheck.AccessibleName=UiText.Pick("Check for updates (not connected yet)","Tikrinti atnaujinimus (dar neprijungta)","Проверить обновления (пока недоступно)");
        resourceExplanation.Text=UiText.Pick("CPU: this program’s share of total processor capacity.\nRAM: memory currently in use, in megabytes (MB)\nAverage background CPU: calculated when the window reopens, from CPU time accumulated by Windows while it was hidden. No periodic sampling runs while hidden.","CPU: šios programos naudojama bendro procesoriaus pajėgumo dalis.\nRAM: naudojama darbinė atmintis megabaitais (MB)\nVid. CPU fone: apskaičiuojamas vėl atidarius langą pagal „Windows“ sukauptą procesoriaus laiką, kol langas buvo paslėptas. Paslėpus periodinis matavimas nevyksta.","CPU: доля общей мощности процессора, используемая программой.\nRAM: рабочая память в мегабайтах (MB)\nСредняя фоновая нагрузка: вычисляется при открытии окна по времени CPU, накопленному Windows, пока окно было скрыто. В фоне периодических измерений нет.");
        if (tray.ContextMenuStrip is { } menu)
            {
                menu.Items[0].Text = UiText.T("Open BlakiBridge");
                menu.Items[1].Text = UiText.Pick("Enable program", "Įjungti programą", "Включить программу");
                menu.Items[2].Text = UiText.T("Optimize mouse speed");
                menu.Items[3].Text = UiText.T("Use physical gap");
                menu.Items[4].Text = UiText.Pick("Close program", "Uždaryti programą", "Закрыть программу");
            }
        UpdateStatus(); FitBands(); UpdateResourceMeter();
    }
    private void Initialize()
    {
        monitors = MonitorModel.Detect(); canvas.Monitors = monitors;
        MonitorIdentity.Restore(config, canvas, monitors);
        canvas.AutoPhysicalSize = config.AutoPhysicalSize;
        foreach (var pair in config.Measurements) canvas.Measurements[pair.Key] = pair.Value;
        foreach (var pair in config.DiagramWidths) canvas.ManualWidths[pair.Key] = pair.Value;
        foreach (var pair in config.DiagramPositions) canvas.DiagramPositions[pair.Key] = pair.Value;
        bool first = config.Transitions.Count == 0;
        // Older configured installations already completed their first-run setup.
        config.WelcomeGuideShown ??= !first || config.MouseControlAdviceShown;
        foreach (var m in monitors) canvas.Positions[m.DeviceName] = config.MonitorPositions.GetValueOrDefault(m.DeviceName, m.Bounds.Location);
        RebuildMaps(); canvas.Invalidate();
        config.BridgeEnabled = !config.TopologyReviewRequired && config.BridgeEnabled;
        updating = true; enabled.Checked = config.BridgeEnabled; adjust.Checked = false; optimize.Checked = config.OptimizeMouseSpeed; useGap.Checked = config.UsePhysicalGap; updating = false;
        if (!diagnostics) BridgeTrace.Write("startup_state", new { config.BridgeEnabled, config.OptimizeMouseSpeed, config.UsePhysicalGap, displays = monitors.Select(m => new { m.DeviceName, m.Bounds, m.DpiX, m.DpiY }) });
        recovery.SetBaseline(monitors);
        initialized = true;
        ShowMarkers(); ApplyEngine(); UpdateStatus(); Save();
        if(!diagnostics && !config.MouseControlAdviceShown)
        {
            config.MouseControlAdviceShown=true;Save();
            tray.ShowBalloonTip(5000,"BlakiBridge",UiText.Pick("Use only one mouse correction program at a time to avoid conflicting movement.","Kad pelės judėjimo korekcijos nesidubliuotų, vienu metu naudokite tik vieną tokią programą.","Во избежание конфликтов используйте только одну программу коррекции движения мыши."),ToolTipIcon.Info);
        }
        if (startHidden) { adjust.Checked = false; Hide(); }
        else ShowWelcomeGuide();
    }
    private void ShowWelcomeGuide()
    {
        if (config.WelcomeGuideShown != false) return;
        ShowPage(guide);
        config.WelcomeGuideShown = true;
        Save();
    }
    internal static List<Transition> BuildMaps(IReadOnlyList<MonitorModel> monitors, Dictionary<string, Point> positions, IEnumerable<Transition> previous)
    {
        var result = new List<Transition>();
        var old = previous.ToList();
        for (int i = 0; i < monitors.Count; i++)
        {
            var (j, side) = AutoAlignment.FindNeighbor(i, monitors, positions);
            if (j < 0) continue;
            var a = monitors[i]; var b = monitors[j];
            if (result.Any(t => (t.A == a.DeviceName && t.B == b.DeviceName) || (t.A == b.DeviceName && t.B == a.DeviceName))) continue;
            var existing = old.FirstOrDefault(t => t.Valid && ((t.A == a.DeviceName && t.B == b.DeviceName && t.Side == side && t.SizeA == a.Bounds.Size && t.SizeB == b.Bounds.Size)
                || (t.B == a.DeviceName && t.A == b.DeviceName && t.Side == Transition.Opposite(side) && t.SizeB == a.Bounds.Size && t.SizeA == b.Bounds.Size)));
            if (existing is not null) { result.Add(existing); continue; }
            var pa = positions.GetValueOrDefault(a.DeviceName, a.Bounds.Location); var pb = positions.GetValueOrDefault(b.DeviceName, b.Bounds.Location);
            bool vertical = Transition.Vertical(side);
            int offsetA = vertical ? pa.Y : pa.X, offsetB = vertical ? pb.Y : pb.X;
            int lengthA = vertical ? a.Bounds.Height : a.Bounds.Width, lengthB = vertical ? b.Bounds.Height : b.Bounds.Width;
            int start = Math.Max(offsetA, offsetB), end = Math.Min(offsetA + lengthA - 1, offsetB + lengthB - 1);
            var map = new Transition { A = a.DeviceName, B = b.DeviceName, Side = side, SizeA = a.Bounds.Size, SizeB = b.Bounds.Size, StartA = start - offsetA, EndA = end - offsetA, StartB = start - offsetB, EndB = end - offsetB };
            if (map.Valid) result.Add(map);
        }
        return result;
    }
    private void RebuildMaps()
    {
        markers?.Dispose(); markers = null;
        var previous=config.Transitions;
        config.Connections ??= MonitorTopology.Suggest(canvas);
        var calibration=MonitorIdentity.Calibration(config,monitors).ToArray();
        var safeLinks=config.Connections.Where(link=>new[]{link.A,link.B}.All(name=>
            monitors.Any(m=>m.DeviceName==name && (!config.MonitorIdentities.TryGetValue(name,out var identity)
                || string.Equals(identity,m.DeviceIdentity,StringComparison.OrdinalIgnoreCase)))));
        config.Transitions=MonitorTopology.Build(canvas,safeLinks,previous.Concat(calibration));
        bool changed=previous.Count>0 && (previous.Count!=config.Transitions.Count || config.Transitions.Any(t=>!previous.Contains(t)));
        if(changed){config.TopologyReviewRequired=true;config.BridgeEnabled=false;updating=true;enabled.Checked=false;updating=false;}
        if(config.UsePhysicalGap && GapSetup.Missing(config.Transitions,config.Gaps).Count>0){config.UsePhysicalGap=false;updating=true;useGap.Checked=false;updating=false;}
        canvas.ConfigureGaps(config.Transitions, config.Gaps, config.UsePhysicalGap);
    }
    internal DisplaySettingsForm? DisplayEditor => displayEditor;
    private void SelectDisplay(int index)
    {
        if(index<0 || index>=monitors.Count)return;
        selectedDisplay=index;
        screenNavigation.SetMonitor($"{index+1} · {monitors[index].ModelName}");
        foreach(var tab in screenNavigation.Tabs)tab.Enabled=true;
    }
    internal void EditDisplay(int index)
    {
        if (recovery.Pending) return;
        if (displayEditor is { IsDisposed: false }) { ShowPage(displayEditor); return; }
        adjust.Checked=false;
        selectedDisplay=index;
        var monitor = monitors[index];
        screenNavigation.SetMonitor($"{index+1} · {monitor.ModelName}");
        foreach(var tab in screenNavigation.Tabs)tab.Enabled=true;
        var dialog = new DisplaySettingsForm(monitor, index + 1, config.Measurements.GetValueOrDefault(monitor.DeviceName), canvas.DisplaySize(monitor), config.MouseSpeedPercent.GetValueOrDefault(monitor.DeviceName,100), config.OptimizeMouseSpeed);
        dialog.AttachConnections(new ConnectionsPage(monitor.DeviceName,monitors,config.Connections ?? [],config.Gaps));
        displayEditor = dialog;
        dialog.SpeedPreviewChanged += value => config.MouseSpeedPreview[monitor.DeviceName] = value;
        dialog.FormClosed += (_, _) =>
        {
            config.MouseSpeedPreview.Remove(monitor.DeviceName);
            displayEditor = null;
            if (dialog.DialogResult == DialogResult.OK)
            {
                config.MouseSpeedPercent[monitor.DeviceName] = dialog.SpeedPercent;
                if(dialog.ConnectionsDraft is {} draft){config.Connections=draft.Links;config.Gaps=draft.Gaps;RebuildMaps();ApplyEngine();}
                if (dialog.MeasurementsChanged)
                {
                    config.Measurements[monitor.DeviceName] = dialog.Result;
                    canvas.Measurements[monitor.DeviceName] = dialog.Result;
                    canvas.ManualWidths.Remove(monitor.DeviceName); config.DiagramWidths.Remove(monitor.DeviceName);
                    canvas.Invalidate(); ApplyEngine();
                }
                Save();
            }
            footerBand.Controls.Remove(dialog.ActionBar); dialog.Controls.Add(dialog.ActionBar);
            workspace.Controls.Remove(dialog); dialog.Dispose();
            ShowPage(screensPage);
        };
        dialog.TopLevel = false; dialog.FormBorderStyle = FormBorderStyle.None;
        dialog.MinimumSize = Size.Empty; dialog.Dock = DockStyle.Fill; dialog.ShowInTaskbar = false;
        workspace.Controls.Add(dialog);
        dialog.Controls.Remove(dialog.ActionBar); dialog.ActionBar.Dock = DockStyle.None;
        dialog.ActionBar.AutoSize = true; dialog.ActionBar.Margin = Padding.Empty; dialog.ActionBar.Padding = Padding.Empty;
        dialog.ActionBar.Font = footerActions.Font;
        footerBand.Controls.Add(dialog.ActionBar); footerBand.Controls.SetChildIndex(dialog.ActionBar,2);
        dialog.Show();
        dialog.ActionBar.Font = footerActions.Font; foreach(Control button in dialog.ActionBar.Controls) { button.Font = footerActions.Font; button.Size = button.GetPreferredSize(Size.Empty); }
        ShowPage(dialog);
    }
    private void CommitGapMode(bool value)
    {
        updating=true; useGap.Checked=value; updating=false;
        config.UsePhysicalGap=value; canvas.ConfigureGaps(config.Transitions,config.Gaps,value); Save(); ApplyEngine();
        if(!diagnostics)BridgeTrace.Write("gap_mode",new{enabled=value});
    }
    private void RequestGapMode(bool requested)
    {
        if(!requested){CommitGapMode(false);return;}
        var missing=GapSetup.Missing(config.Transitions,config.Gaps);
        if(missing.Count==0 && config.Transitions.Count>0){CommitGapMode(true);return;}
        CommitGapMode(false);
        if(monitors.Count>0){int index=missing.Count>0?monitors.ToList().FindIndex(m=>m.DeviceName==missing[0].A):0;EditDisplay(Math.Max(0,index));displayEditor?.SelectTab(2);}
    }
    private void OpenGapEditor(Transition pair,string action,string? progress,Action<bool> complete)
    {
        string key=GapSettings.Key(pair.A,pair.B);
        void Commit(decimal value){config.Gaps[key]=new GapSettings{EdgeDistanceMm=value};canvas.ConfigureGaps(config.Transitions,config.Gaps,config.UsePhysicalGap);Save();}
        if(diagnostics && DiagnosticGapEditor is not null)
        {var value=DiagnosticGapEditor(pair,action,progress);if(value is decimal saved)Commit(saved);complete(value is not null);return;}
        if(recovery.Pending){complete(false);return;}
        displayEditor?.Close();gapEditor?.Close();
        var dialog=new PairGapForm(pair,monitors,config.Gaps.GetValueOrDefault(key)?.EdgeDistanceMm,action,progress);
        gapEditor=dialog;
        dialog.FormClosed+=(_,_)=>
        {
            bool saved=dialog.DialogResult==DialogResult.OK;if(saved)Commit(dialog.Distance);
            gapEditor=null;footerBand.Controls.Remove(dialog.ActionBar);dialog.Controls.Add(dialog.ActionBar);
            workspace.Controls.Remove(dialog);dialog.Dispose();ShowPage(screensPage);complete(saved);
        };
        dialog.TopLevel=false;dialog.FormBorderStyle=FormBorderStyle.None;dialog.MinimumSize=Size.Empty;dialog.Dock=DockStyle.Fill;dialog.ShowInTaskbar=false;
        workspace.Controls.Add(dialog);dialog.Controls.Remove(dialog.ActionBar);dialog.ActionBar.Dock=DockStyle.None;
        dialog.ActionBar.AutoSize=true;dialog.ActionBar.Margin=Padding.Empty;dialog.ActionBar.Padding=Padding.Empty;dialog.ActionBar.Font=footerActions.Font;
        footerBand.Controls.Add(dialog.ActionBar);footerBand.Controls.SetChildIndex(dialog.ActionBar,2);dialog.Show();
        foreach(Control button in dialog.ActionBar.Controls){button.Font=footerActions.Font;button.Size=button.GetPreferredSize(Size.Empty);}
        ShowPage(dialog);
    }
    internal void EditGap(Transition pair) {int index=monitors.ToList().FindIndex(m=>m.DeviceName==pair.A);if(index>=0){EditDisplay(index);displayEditor?.SelectTab(2);}}
    private void ShowMarkers()
    {
        if (diagnostics) return;
        if (escapeKey) { UnregisterHotKey(Handle, 913); escapeKey = false; }
        markers?.Dispose(); markers = null;
        if (enabled.Checked && adjust.Checked && !closing && !recovery.Pending)
        {
            markers = new LiveMarkers(this, config.Transitions, monitors, value => { dragging = value; if (!closing) ApplyEngine(); }, Save);
            escapeKey = RegisterHotKey(Handle, 913, 0x4000, 0x1B);
        }
        UpdateStatus();
    }
    private void Save()
    {
        if (!initialized) return;
        foreach(var pair in canvas.Positions)config.MonitorPositions[pair.Key]=pair.Value;
        foreach(var monitor in monitors)
        {
            if(canvas.ManualWidths.TryGetValue(monitor.DeviceName,out var width))config.DiagramWidths[monitor.DeviceName]=width;
            else config.DiagramWidths.Remove(monitor.DeviceName);
        }
        foreach(var pair in canvas.DiagramPositions)config.DiagramPositions[pair.Key]=pair.Value;
        _ = MonitorIdentity.Calibration(config,monitors).ToArray();
        if (diagnostics && DiagnosticSave is null) return;
        try
        {
            if(DiagnosticSave!=null)DiagnosticSave();else config.Save();
            saveFailure=null;UpdateIssue();UpdateActivity();
            BridgeTrace.Write("config_saved", new { config.Language, positions = config.MonitorPositions.Count, widths = config.DiagramWidths.Count, transitions = config.Transitions.Count });
            status.Text = UiText.T("Saved"); notice.Stop(); notice.Start();
        }
        catch (Exception ex)
        {
            BridgeTrace.Write("config_save_failed", new { error = ex.Message });
            notice.Stop();saveFailure=UiText.Pick("Could not save settings. Retry saving.","Nepavyko išsaugoti nustatymų. Pabandykite dar kartą.","Не удалось сохранить настройки. Повторите сохранение.");
            UpdateIssue();UpdateActivity();AnnounceIssue(saveFailure);
        }
    }
    private void ApplyEngine()
    {
        if (recovery.Pending) { StopEngine(); UpdateStatus(); return; }
        if (diagnostics) { if (!closing && enabled.Checked && !dragging) DiagnosticEngineStarts++; UpdateActivity(); return; }
        if (dragging && enabled.Checked && !closing) { bridge?.Dispose(); bridge = null; return; }
        StopEngine();
        if (closing || !enabled.Checked || dragging) { if(!config.BridgeEnabled)engineFailure=null;UpdateStatus(); return; }
        try
        {
            if (config.Transitions.Count == 0) throw new InvalidOperationException("Place screens beside each other to create a connection.");
            hotkey = RegisterHotKey(Handle, 912, 0x4003, 0x7B);
            if (!hotkey) throw new InvalidOperationException("Ctrl + Alt + F12 is unavailable.");
            bridge = new MouseBridge(config.Transitions, monitors, config);bridge.Faulted+=EngineFailed; bridge.Start();engineFailure=null; UpdateStatus();
            BridgeTrace.Write("engine_started", new { config.OptimizeMouseSpeed, config.UsePhysicalGap });
        }
        catch (Exception ex)
        {
            BridgeTrace.Write("engine_start_failed", new { error = ex.ToString(), desiredEnabled = config.BridgeEnabled });
            StopEngine(); updating = true; enabled.Checked = false; updating = false;
            engineFailure=UiText.T(ex.Message);UpdateStatus();AnnounceIssue(engineFailure);
        }
    }
    private bool recoveryNotified;
    private void BeginRecovery(bool suspended)
    {
        recovery.Begin(suspended); recoveryNotified=false; recoveryTimer.Stop(); recoveryTimer.Interval=1000; displayEditor?.Close(); gapEditor?.Close();
        notice.Stop(); markers?.Dispose(); markers = null;
        if (escapeKey) { UnregisterHotKey(Handle, 913); escapeKey = false; }
        updating = true; adjust.Checked = false; updating = false;
        dragging = false; StopEngine();
        canvas.CancelInteraction(); canvas.Enabled = false; adjust.Enabled = false;
        if ((!diagnostics || DiagnosticUseRecoveryTimer) && recovery.ShouldPoll) recoveryTimer.Start();
        if (!diagnostics) BridgeTrace.Write("recovery_wait", new { suspended, desiredEnabled = config.BridgeEnabled });
        UpdateStatus();
    }
    private void PollRecovery()
    {
        if (closing || !recovery.ShouldPoll) { recoveryTimer.Stop(); return; }
        try { ProcessRecoverySnapshot(MonitorModel.DetectRecoverySnapshot(monitors)); }
        catch (Exception ex)
        {
            // Detection failure consumes a bounded attempt and cannot start a stale mapping.
            recovery.Observe([]);
            if (!diagnostics) BridgeTrace.Write("recovery_detect_failed", new { error = ex.Message });
        }
        if (!recovery.ShouldPoll) recoveryTimer.Stop();else recoveryTimer.Interval=recovery.PollInterval;
        UpdateStatus();
        if(!diagnostics && recovery.Pending && !recoveryNotified && recovery.Attempts>=3)
        {
            recoveryNotified=true;tray.ShowBalloonTip(5000,"BlakiBridge",UiText.Pick("Monitor configuration changed. Mouse correction is paused. Open BlakiBridge to check your screens.","Pasikeitė ekranų konfigūracija. Pelės korekcija pristabdyta. Atverkite BlakiBridge ir patikrinkite ekranus.","Конфигурация экранов изменилась. Коррекция мыши приостановлена. Откройте BlakiBridge и проверьте экраны."),ToolTipIcon.Warning);
        }
    }
    internal void ProcessRecoverySnapshot(IReadOnlyList<MonitorModel> current)
    {
        if (!recovery.Observe(current)) return;
        recoveryTimer.Stop();recoveryNotified=false;
        if(recovery.AcceptedDifferentSetup)AcceptReviewedScreens(current);
        if(recovery.Renames.Count>0){MonitorRename.Apply(config,canvas,recovery.Renames);canvas.ConfigureGaps(config.Transitions,config.Gaps,config.UsePhysicalGap);selectedDisplay=-1;screenNavigation.SetMonitor("");foreach(var tab in screenNavigation.Tabs)tab.Enabled=false;if(!diagnostics)BridgeTrace.Write("recovery_monitor_renamed",new{names=recovery.Renames});Save();}
        recovery.SetBaseline(current);
        monitors = current; canvas.Monitors = current; canvas.Enabled = true; adjust.Enabled = enabled.Checked;
        MonitorIdentity.Restore(config,canvas,current);
        // Automatic recovery preserves maps; explicit review rebuilds only a changed setup.
        updating = true; enabled.Checked = config.BridgeEnabled; updating = false;
        canvas.Invalidate(); ApplyEngine();UpdateStatus();
        if (!diagnostics) BridgeTrace.Write("recovery_complete", new { desiredEnabled = config.BridgeEnabled, active = bridge is not null, attempts = recovery.Attempts });
    }
    private void StopEngine()
    {
        if (bridge is not null && !diagnostics) BridgeTrace.Write("engine_stopped");
        bridge?.Dispose(); bridge = null;
        if (hotkey) { UnregisterHotKey(Handle, 912); hotkey = false; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { recoveryTimer.Dispose(); resourceTimer.Dispose(); resourceMeter.Dispose();  }
        base.Dispose(disposing);
    }
    private void UpdateActivity()
    {
        adjust.Enabled = enabled.Checked && !recovery.Pending && !closing;
        if (!adjust.Enabled && adjust.Checked) adjust.Checked = false;
        bool active = !recovery.Pending && (diagnostics ? enabled.Checked : bridge is {IsRunning:true});
        bool waiting = !active && config.BridgeEnabled;
        activityDot.ForeColor = active ? Color.FromArgb(126,220,42) : waiting ? Color.FromArgb(255,153,0) : Color.Gray;
        UpdateTrayState(active,waiting);
        activityText.Text = active ? UiText.Pick("Enabled", "Įjungta", "Включено") : waiting ? UiText.Pick("Paused", "Pristabdyta", "Приостановлено") : UiText.Pick("Off", "Išjungta", "Выключено");
    }
    private void UpdateStatus()
    {
        UpdateActivity();UpdateIssue();
        if (recovery.Pending)
        {
            status.Text = UiText.Pick("Waiting for calibrated screens. Use Check screens to review the current setup.", "Laukiama sukalibruotų ekranų. Dabartinę konfigūraciją peržiūrėkite pasirinkę „Patikrinti ekranus“.", "Ожидание откалиброванных экранов. Выберите «Проверить экраны» для проверки текущей конфигурации.");
            tray.Text = UiText.Pick("BlakiBridge - waiting for screens", "BlakiBridge - laukiama ekranų", "BlakiBridge - ожидание экранов");
            return;
        }
        bool missingPair=config.Connections?.Any(link=>!config.Transitions.Any(t=>GapSettings.Key(t.A,t.B)==GapSettings.Key(link.A,link.B)))==true;
        status.Text = missingPair ? UiText.Pick("A saved monitor pair is unavailable. Check the monitor connection settings.","Išsaugota monitorių pora nepasiekiama. Patikrinkite monitorių ryšių nustatymus.","Сохранённая пара мониторов недоступна. Проверьте настройки связей мониторов.") : config.TopologyReviewRequired ? UiText.Pick("Monitor pairs changed. Check Adjust, then enable the program.","Monitorių poros pakeistos. Patikrinkite „Reguliuoti“, tada įjunkite programą.","Пары мониторов изменены. Проверьте калибровку и включите программу.") : UiText.T("Arrange the screens visually to match your real monitor setup.");
        if (!diagnostics && adjust.Checked && !escapeKey) status.Text = UiText.T("Esc is in use by another app. Turn off Adjust here to hide the lines.");
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        powerNotification = RegisterSuspendResumeNotification(Handle, 0);
        if (!diagnostics) BridgeTrace.Write("power_notifications", new { registered = powerNotification != IntPtr.Zero, error = powerNotification == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0 });
    }
    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (powerNotification != IntPtr.Zero) { UnregisterSuspendResumeNotification(powerNotification); powerNotification = IntPtr.Zero; }
        base.OnHandleDestroyed(e);
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr RegisterSuspendResumeNotification(IntPtr recipient, uint flags);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterSuspendResumeNotification(IntPtr notification);
    protected override void WndProc(ref Message m)
    {
        if (WindowHeader.HandleFrame(this, ref m)) return;
        if (m.Msg == 0x312 && m.WParam == (IntPtr)913) adjust.Checked = false;
        if (m.Msg == 0x312 && m.WParam == (IntPtr)912)
        {
            if (!diagnostics) BridgeTrace.Write("emergency_shortcut");
            enabled.Checked = false; adjust.Checked = false;
        }
        if (!diagnostics && (m.Msg == 0x7E || m.Msg == 0x218 || m.Msg == 0x219))
            BridgeTrace.Write("windows_display_power_device_event", new { message = m.Msg, wparam = m.WParam.ToInt64(), lparam = m.LParam.ToInt64(), foregroundProcess = BridgeTrace.ForegroundProcess(), actualEnabled = enabled.Checked, desiredEnabled = config.BridgeEnabled, displays = Screen.AllScreens.Select(s => new { s.DeviceName, s.Bounds }) });
        if (initialized && !closing)
        {
            if (m.Msg == 0x218 && m.WParam == (IntPtr)4) BeginRecovery(true);
            else if (m.Msg == 0x218 && (m.WParam == (IntPtr)18 || m.WParam == (IntPtr)7)) BeginRecovery(false);
            else if (m.Msg == 0x7E && !recovery.Pending) BeginRecovery(false);
            // Device event bursts do not reset the current stability window or retry budget.
        }
        base.WndProc(ref m);
    }
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr window, int id);
}
