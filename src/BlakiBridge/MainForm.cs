namespace BlakiBridge;

public sealed class MainForm : Form
{
    private readonly MonitorCanvas _canvas = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _cards = new() { Dock = DockStyle.Bottom, Height = 118, Padding = new Padding(16), AutoScroll = true, WrapContents = false };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.FromArgb(178,178,178), Text = "Detecting displays…", Padding = new Padding(0, 9, 12, 0) };
    private readonly Button _enable = MakeButton("Align screens", true);
    private AutoAlignment? alignment;
    private MouseBridge? bridge;
    private readonly Button tryButton = MakeButton("Try it", true);
    private readonly System.Windows.Forms.Timer trial = new() { Interval = 1000 };
    private DateTime trialEnd;
    private bool hotkey;
    private IReadOnlyList<MonitorModel> _monitors = [];
    private readonly BridgeConfig _config = BridgeConfig.Load();

    public MainForm()
    {
        Text = "BlakiBridge";
        MinimumSize = new Size(1060, 640);
        Size = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(19,19,19);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        var header = new Panel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(24, 16, 24, 12) };
        var title = new Label { Text = "BlakiBridge", Font = new Font("Segoe UI Semibold", 22), AutoSize = true, Location = new Point(22, 13) };
        var subtitle = new Label { Text = "Drag screens into place. Click a screen to show alignment lines.", ForeColor = Color.FromArgb(178,178,178), AutoSize = true, Location = new Point(25, 55) };
        header.Controls.AddRange([title, subtitle]);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(18, 12, 18, 10) };
        var identify = MakeButton("Identify displays", false);
        var reset = MakeButton("Restore Windows layout", false);
        var save = MakeButton("Save layout", false);
        identify.Click += (_, _) => Identify();
        reset.Click += (_, _) => { _canvas.ResetFromWindows(); SaveLayout(); };
        save.Click += (_, _) => SaveLayout();
        _enable.Click += (_, _) => ShowCalibrationInfo();
        actions.Controls.AddRange([tryButton, _enable, reset, identify]);
        tryButton.Click += (_, _) => ToggleTrial();
        trial.Tick += (_, _) =>
        {
            if (DateTime.UtcNow >= trialEnd) { StopTrial(); return; }
            _status.Text = $"Try it: {Math.Ceiling((trialEnd - DateTime.UtcNow).TotalSeconds)} seconds · Ctrl + Alt + F12 stops mouse control";
        };
        _status.Dock = DockStyle.Bottom; _status.AutoSize = false; _status.Height = 36; _status.Padding = new Padding(20, 8, 0, 0);

        _cards.BackColor = Color.FromArgb(29,29,29);
        _canvas.LayoutChanged += (_, _) => SaveLayout();
        _canvas.DisplayClicked += StartAlignment;
        Controls.AddRange([_canvas, _status, actions, header]);
        FormClosed += (_, _) => { alignment?.Dispose(); StopTrial(); trial.Dispose(); };
        Shown += (_, _) => RefreshMonitors();
    }

    private void RefreshMonitors()
    {
        _monitors = MonitorModel.Detect();
        _canvas.Monitors = _monitors;
        foreach (var m in _monitors)
            _canvas.Positions[m.DeviceName] = _config.MonitorPositions.GetValueOrDefault(m.DeviceName, new Point(m.Bounds.X, m.Bounds.Y));
        _cards.Controls.Clear();
        for (int i = 0; i < _monitors.Count; i++) _cards.Controls.Add(CreateCard(_monitors[i], i + 1));
        _status.Text = $"{_monitors.Count} displays detected · Align screens, then click Try it";
        _enable.Enabled = _monitors.Count >= 2;
        _canvas.Invalidate();
    }

    private static Control CreateCard(MonitorModel m, int number)
    {
        string physical = m.WidthMm > 0 ? $"{m.WidthMm} × {m.HeightMm} mm · {m.PhysicalDpiX:0} PPI" : "Physical size unavailable";
        return new Label
        {
            Width = 245, Height = 78, Padding = new Padding(12, 8, 8, 8), Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.FromArgb(47,47,47), ForeColor = Color.White,
            Text = $"{number}. {m.DeviceName}{(m.Primary ? "  · Primary" : "")}\n{m.Bounds.Width} × {m.Bounds.Height} · {m.DpiX} DPI\n{physical}"
        };
    }

    private void Identify()
    {
        var screens = Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++) new IdentifyForm(screens[i], i + 1).Show();
    }

    private void SaveLayout()
    {
        _config.MonitorPositions = new Dictionary<string, Point>(_canvas.Positions);
        try { _config.Save(); _status.Text = "Layout saved"; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save layout"); }
    }

    private void ShowCalibrationInfo()
    {
        int index = _monitors.ToList().FindIndex(m => !m.Primary && !_config.Transitions.Any(t => t.A == m.DeviceName || t.B == m.DeviceName));
        if (index < 0) index = _monitors.ToList().FindIndex(m => !m.Primary);
        if (index >= 0) StartAlignment(index);
    }

    private void StartAlignment(int index)
    {
        StopTrial();
        if (_monitors.Count < 2) return;
        alignment?.Dispose();
        alignment = new AutoAlignment(this, _config, message => _status.Text = message);
        alignment.Start(index, _monitors, _canvas.Positions);
    }

    private void ToggleTrial()
    {
        if (bridge is not null) { StopTrial(); return; }
        try
        {
            alignment?.Dispose();
            foreach (var process in System.Diagnostics.Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        if (process.ProcessName.Contains("LittleBigMouse", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Little Big Mouse is running. Exit it before trying BlakiBridge.");
                    }
                    catch (System.ComponentModel.Win32Exception) { }
                }
            }
            var current = MonitorModel.Detect();
            if (_monitors.Any(m => !current.Any(n => n.DeviceName == m.DeviceName && n.Bounds == m.Bounds)))
                throw new InvalidOperationException("Displays changed. Restart BlakiBridge and align again.");
            var candidate = new MouseBridge(_config.Transitions, current);
            hotkey = RegisterHotKey(Handle, 912, 0x4003, 0x7B);
            if (!hotkey) { candidate.Dispose(); throw new InvalidOperationException("Ctrl + Alt + F12 is unavailable. Close the application using this shortcut and try again."); }
            bridge = candidate; bridge.Start();
            trialEnd = DateTime.UtcNow.AddSeconds(30); trial.Start();
            tryButton.Text = "Stop";
            _status.Text = "Mouse control active for 30 seconds · Ctrl + Alt + F12 stops it";
        }
        catch (Exception ex) { StopTrial(); MessageBox.Show(this, ex.Message, "Try it"); }
    }
    private void StopTrial()
    {
        trial.Stop(); bridge?.Dispose(); bridge = null;
        if (hotkey) { UnregisterHotKey(Handle, 912); hotkey = false; }
        tryButton.Text = "Try it";
        if (!IsDisposed) _status.Text = "Mouse control off";
    }
    protected override void WndProc(ref Message m)
    {
        if ((m.Msg == 0x312 && m.WParam == (IntPtr)912) || m.Msg == 0x7E || (m.Msg == 0x218 && m.WParam == (IntPtr)4)) StopTrial();
        base.WndProc(ref m);
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr window, int id);

    private static Button MakeButton(string text, bool accent)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 38, FlatStyle = FlatStyle.Flat, Padding = new Padding(12, 0, 12, 0), Margin = new Padding(8, 0, 0, 0), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = accent ? 0 : 1;
        b.FlatAppearance.BorderColor = Color.FromArgb(81,81,81);
        b.BackColor = accent ? Color.FromArgb(192, 105, 25) : Color.FromArgb(44,44,44);
        b.ForeColor = Color.White;
        return b;
    }
}
