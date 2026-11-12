namespace BlakiBridge;

public sealed class DisplayMeasurements
{
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Left { get; set; }
    public decimal Right { get; set; }
    public decimal Top { get; set; }
    public decimal Bottom { get; set; }
    public bool Valid => Width >= 30 && Width <= 4000 && Height >= 30 && Height <= 4000
        && new[] { Left, Right, Top, Bottom }.All(v => v >= 0 && v <= 200);
    public decimal Bezel(Edge edge) => edge switch { Edge.Left => Left, Edge.Right => Right, Edge.Top => Top, _ => Bottom };
}
public sealed class GapSettings
{
    public decimal? EdgeDistanceMm { get; set; }
    public bool HasDistance => EdgeDistanceMm is >= 0 and <= 2000;
    public decimal Millimeters { get; set; }
    public bool Compensate { get; set; }
    public static string Key(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
}

public sealed class DisplaySettingsForm : ThemedForm
{
    private readonly DisplayNavigation navigation = new();
    internal BackButton Back => navigation.Back;
    private readonly Panel tabArea = new() { Dock = DockStyle.Fill };
    private PageTabButton dimensionsTab => navigation.Tabs[0];
    private PageTabButton speedTab => navigation.Tabs[1];
    private Panel dimensionsPage = null!, speedPage = null!;
    internal ConnectionsPage? ConnectionsDraft;
    private bool connectionsVisited;
    private PageTabButton gapsTab => navigation.Tabs[2];
    internal void AttachConnections(ConnectionsPage page)
    {
        ConnectionsDraft=page;page.Visible=false;tabArea.Controls.Add(page);
        gapsTab.Text=UiText.T("Physical gap");gapsTab.Click+=(_,_)=>SelectTab(2);
    }
    internal FlowLayoutPanel ActionBar { get; private set; } = null!;
    internal void SelectTab(int index)
    {
        dimensionsPage.Visible = index == 0; speedPage.Visible = index == 1;
        if(ConnectionsDraft!=null){ConnectionsDraft.Visible=index==2;gapsTab.Selected=index==2;gapsTab.Invalidate();if(index==2)connectionsVisited=true;}
        (index == 0 ? dimensionsPage : index==1 ? speedPage : (Control?)ConnectionsDraft ?? dimensionsPage).BringToFront();
        dimensionsTab.Selected = index == 0; speedTab.Selected = index == 1; dimensionsTab.Invalidate(); speedTab.Invalidate();
        dimensionsTab.BackColor = index == 0 ? Color.FromArgb(60,48,32) : BackColor;
        speedTab.BackColor = index == 1 ? Color.FromArgb(60,48,32) : BackColor;
        dimensionsTab.ForeColor = index == 0 ? Color.DarkOrange : ForeColor;
        speedTab.ForeColor = index == 1 ? Color.DarkOrange : ForeColor;

    }
    internal readonly SpeedPercentInput SpeedInput = new() { Minimum = 25, Maximum = 300, Value = 100, Increment = 5, Width = 140, BackColor = Color.FromArgb(39,39,39), ForeColor = Color.WhiteSmoke };
    internal event Action<decimal>? SpeedPreviewChanged;
    private decimal[] initialMeasurements = [];
    internal bool MeasurementsChanged => values.Select(v => v.Value).Where((v,i) => v != initialMeasurements[i]).Any();
    internal decimal SpeedPercent => SpeedInput.Value;
    private string monitorName = "";
    private readonly SmoothLabel speedHint = new() { AutoSize = true, MaximumSize = new Size(780,0), Margin = new Padding(0,18,0,0) };
    internal void SetSpeedEnabled(bool enabled)
    {
        SpeedInput.Enabled = enabled;
        speedHint.Text = enabled
            ? UiText.Pick($"Move the pointer onto {monitorName} and between monitors to adjust the perceived mouse speed. Changes apply immediately. When it feels right, click Save.", $"Perkelkite pelės žymeklį į monitorių „{monitorName}“ ir, judindami jį tarp monitorių, sureguliuokite juntamą pelės greitį. Pakeitimai taikomi iškart. Radę tinkamą nustatymą, paspauskite „Išsaugoti“.", $"Перемещайте указатель на {monitorName} и между мониторами, чтобы настроить ощущаемую скорость мыши. Изменения применяются сразу. Подобрав скорость, нажмите «Сохранить».")
            : UiText.Pick("Enable Optimize mouse speed to adjust and try it.", "Įjunkite „Vienodinti pelės greitį“, kad galėtumėte reguliuoti ir išbandyti.", "Включите выравнивание скорости мыши для настройки и проверки.");
    }
    private readonly RoundedNumber[] values = new RoundedNumber[6];
    public DisplayMeasurements Result => new() { Width = values[0].Value, Height = values[1].Value, Left = values[2].Value, Right = values[3].Value, Top = values[4].Value, Bottom = values[5].Value };
    internal readonly MonitorPreview Preview = new() { Dock = DockStyle.Fill };
    internal void SelectParameter(int index) => Preview.SelectParameter(index);
    internal void SetParameter(int index, decimal value) => values[index].Value = value;
    public DisplaySettingsForm(MonitorModel monitor, int number, DisplayMeasurements? saved, SizeF? estimate = null, decimal speedPercent = 100, bool speedEnabled = false)
    {
        monitorName = string.IsNullOrWhiteSpace(monitor.ModelName) ? UiText.T("Display") + " " + number : monitor.ModelName;
        Preview.MonitorName = monitorName;
        Text = $"{UiText.T("Display")} {number} · {UiText.Pick("Settings", "Nustatymai", "Настройки")}"; StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1140, 640); MinimumSize = new Size(1156, 679); MaximizeBox = false; MinimizeBox = false;
        Font = new Font("Segoe UI", 11); BackColor = AppTheme.Background; ForeColor = Color.White;
        var body = new BufferedTableLayout { Dock = DockStyle.Fill, Padding = new Padding(22,12,22,20), ColumnCount = 2, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var layout = new BufferedTableLayout { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 6, Padding = new Padding(12, 8, 12, 0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        var info = new SmoothLabel { Dock = DockStyle.Fill, Padding = new Padding(2,4,2,4), TextAlign = ContentAlignment.MiddleLeft, Text = UiText.Pick("Optional precision · all values in mm.\nHover over a parameter to locate it in the preview. Changes apply after saving.", "Papildomas tikslumas · visi dydžiai mm.\nUžveskite pelę ant parametro, kad matytumėte jo vietą. Pakeitimai taikomi išsaugojus.", "Уточнение размеров · все значения в мм.\nНаведите на параметр для просмотра. Изменения применяются после сохранения.") };
        var source = new SmoothLabel { Dock = DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, Text = monitor.SizeSources.Replace("\n", "\r\n"), BackColor = AppTheme.Surface, ForeColor = Color.Silver, Font = new Font("Segoe UI",9), Margin = new Padding(12,0,12,0) };
        var information = new BufferedTableLayout { Dock = DockStyle.Top, Height = 96, ColumnCount = 2, Padding = Padding.Empty, BackColor = AppTheme.Surface };
        information.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60)); information.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));
        information.Controls.Add(info,0,0); information.Controls.Add(source,1,0);
        string[] labels = ["Active image width", "Active image height", "Left bezel", "Right bezel", "Top bezel", "Bottom bezel"];
        layout.Paint += (_, e) => { var rows = layout.GetRowHeights(); if(rows.Length>1){int y=layout.Padding.Top+rows[0]+rows[1]-8;using var pen=new Pen(AppTheme.Border);e.Graphics.DrawLine(pen,12,y,layout.Width-12,y);} };
        var initial = estimate ?? DisplaySizing.Resolve(monitor, new Dictionary<string, float>(), new Dictionary<string, DisplayMeasurements>());
        decimal[] defaults = saved is { Valid: true } ? [saved.Width, saved.Height, saved.Left, saved.Right, saved.Top, saved.Bottom] : [(decimal)initial.Width, (decimal)initial.Height, 0, 0, 0, 0];
        for (int i = 0; i < 6; i++)
        {
            int parameter = i;
            var label = new SmoothLabel { Text = labels[i], AutoSize = true, Margin = new Padding(0, 5, 0, 6) };
            layout.Controls.Add(label, 0, i);
            values[i] = new RoundedNumber { DecimalPlaces = 1, Maximum = i < 2 ? 4000 : 200, Minimum = i < 2 ? 30 : 0, Value = Math.Clamp(defaults[i], i < 2 ? 30 : 0, i < 2 ? 4000 : 200), Dock = DockStyle.Top, Margin = new Padding(3, 0, 3, i == 1 ? 14 : 6), BackColor = Color.FromArgb(21,21,21), ForeColor = Color.WhiteSmoke };
            layout.Controls.Add(values[i], 1, i);
            layout.Controls.Add(new SmoothLabel { Text = "mm", Height = 32, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(7,0,0,0), ForeColor = Color.Silver },2,i);
            void Wire(Control control)
            {
                control.MouseEnter += (_, _) => SelectParameter(parameter);
                control.Enter += (_, _) => SelectParameter(parameter);
                foreach (Control child in control.Controls) Wire(child);
            }
            Wire(label); Wire(values[i]);
            values[i].ValueChanged += (_, _) => { Preview.SetMeasurements(Result); SelectParameter(parameter); };
        }
        var buttons = ActionBar = new LayoutRow { Height = 68, Padding = new Padding(8), FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Bottom };
        var save = new RoundedButton { Text = "Save", AutoSize = true, FlatStyle = FlatStyle.Flat, Padding = new Padding(12,5,12,5), MinimumSize = new Size(88,32), BackColor = Color.DarkOrange, ForeColor = Color.Black };
        var cancel = new RoundedButton { Text = "Cancel", AutoSize = true, FlatStyle = FlatStyle.Flat, Padding = new Padding(12,5,12,5), MinimumSize = new Size(88,32), ForeColor = Color.WhiteSmoke, BackColor = AppTheme.Surface };
        save.Click += (_, _) => { if (!Result.Valid) { MessageBox.Show(this, "Enter image width and height between 30 and 4000 mm."); return; } if(connectionsVisited)ConnectionsDraft?.CommitCurrent(); DialogResult = DialogResult.OK; Close(); };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        save.FlatAppearance.BorderColor = Color.DarkOrange; cancel.FlatAppearance.BorderColor = Color.FromArgb(62,62,62);
        buttons.Controls.AddRange([cancel, save]);
        var fieldsCard = new SettingsCard(UiText.Pick("Screen dimensions", "Ekrano matmenys", "Размеры экрана")) { Dock = DockStyle.Fill, Margin = new Padding(0,0,8,0) };
        var previewCard = new SettingsCard(UiText.Pick("Preview", "Peržiūra", "Просмотр")) { Dock = DockStyle.Fill, Margin = new Padding(8,0,0,0) };
        fieldsCard.Controls.Add(layout); previewCard.Controls.Add(Preview);
        body.Controls.Add(fieldsCard,0,0); body.Controls.Add(previewCard,1,0);
        navigation.SetMonitor($"{number} · {monitorName}");
        dimensionsTab.Text = UiText.Pick("Dimensions", "Matmenys", "Размеры");
        speedTab.Text = UiText.Pick("Mouse speed", "Pelės greitis", "Скорость мыши");
        Back.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        dimensionsTab.Click += (_, _) => SelectTab(0); speedTab.Click += (_, _) => SelectTab(1);
        dimensionsTab.Margin = new Padding(0,0,4,0); speedTab.Margin = new Padding(0,0,4,0);
        Back.Height = 44; Back.Margin = new Padding(0,0,8,0);

        var dimensions = dimensionsPage = new Panel { Dock = DockStyle.Fill, AutoScroll = false, BackColor = BackColor, ForeColor = ForeColor };
        var speed = speedPage = new Panel { Dock = DockStyle.Fill, AutoScroll = false, BackColor = AppTheme.Background, ForeColor = ForeColor };
        dimensions.Controls.AddRange([body, new InformationBand(information)]);
        var speedInformation = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(2,4,2,4), BackColor = AppTheme.Surface };
        speedHint.TextAlign=ContentAlignment.MiddleLeft;speedHint.AutoSize = false; speedHint.Dock = DockStyle.Fill; speedHint.MaximumSize = Size.Empty;
        speedInformation.Controls.Add(speedHint);
        var speedBody = new BufferedTableLayout { Dock = DockStyle.Fill, Padding = new Padding(22,12,22,20), ColumnCount = 2, RowCount = 1 };
        speedBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); speedBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); speedBody.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var speedCard = new SettingsCard(UiText.Pick("Mouse speed", "Pelės greitis", "Скорость мыши")) { Dock = DockStyle.Fill, Margin = new Padding(0,0,8,0) };
        var speedRow = new LayoutRow { Dock = DockStyle.Top, Height = 64, Padding = new Padding(12,8,12,8) };
        SpeedInput.Value = Math.Clamp(speedPercent,25,300); SpeedInput.Margin = new Padding(0,0,8,0);
        var reset = new RoundedButton { Text = UiText.Pick("Reset", "Atkurti", "Сбросить"), AutoSize = true, Margin = new Padding(12,0,0,0) };
        reset.Click += (_, _) => { if (SpeedInput.Enabled) SpeedInput.Value = 100; };
        speedRow.Controls.AddRange([SpeedInput,new InlineUnit { Text="%",Size=new Size(18,32),Margin=Padding.Empty },reset]);
        speedCard.Controls.Add(speedRow); speedBody.Controls.Add(speedCard,0,0);speedBody.SetColumnSpan(speedCard,2);speedCard.Margin=Padding.Empty;
        speed.Controls.AddRange([speedBody,new InformationBand(speedInformation)]);
        tabArea.Controls.AddRange([dimensions, speed]);
        SelectTab(0);
        SpeedInput.ValueChanged += (_, _) => SpeedPreviewChanged?.Invoke(SpeedInput.Value);
        SetSpeedEnabled(speedEnabled);
        initialMeasurements = values.Select(v => v.Value).ToArray();
        Controls.AddRange([tabArea, buttons, navigation]); AcceptButton = save; CancelButton = cancel;
        Preview.SetMeasurements(Result);
        UiText.Apply(this);
    }
}

public sealed class GapsForm : Form
{
    public Dictionary<string, GapSettings> Result { get; } = new();
    public GapsForm(IEnumerable<Transition> maps, IReadOnlyList<MonitorModel> monitors, Dictionary<string, GapSettings> saved)
    {
        Text = "Physical gaps"; StartPosition = FormStartPosition.CenterParent; Size = new Size(650, 420); MinimumSize = new Size(620, 360);
        Font = new Font("Segoe UI", 11); BackColor = AppTheme.Background; ForeColor = Color.White;
        var rows = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = false, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20) };
        rows.Controls.Add(new SmoothLabel { AutoSize = true, MaximumSize = new Size(560, 0), Text = "Gap between the monitor cases (mm). Bezels are added separately. Instant crossing is the default; enable compensation to include the hidden distance.", Margin = new Padding(0, 0, 0, 20) });
        foreach (var map in maps)
        {
            string key = GapSettings.Key(map.A, map.B); var original = saved.GetValueOrDefault(key) ?? new GapSettings();
            var value = new GapSettings { Millimeters = Math.Clamp(original.Millimeters, 0, 2000), Compensate = original.Compensate }; Result[key] = value;
            var row = new FlowLayoutPanel { AutoSize = true };
            int a = monitors.ToList().FindIndex(m => m.DeviceName == map.A) + 1, b = monitors.ToList().FindIndex(m => m.DeviceName == map.B) + 1;
            row.Controls.Add(new SmoothLabel { Text = $"Display {a} ↔ {b}", AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
            var input = new RoundedNumber { DecimalPlaces = 1, Maximum = 2000, Value = value.Millimeters, Width = 100 };
            var check = new CheckBox { Text = "Compensate gap", Checked = value.Compensate, AutoSize = true, Padding = new Padding(10, 3, 0, 0) };
            input.ValueChanged += (_, _) => value.Millimeters = input.Value;
            check.CheckedChanged += (_, _) => value.Compensate = check.Checked;
            row.Controls.AddRange([input, check]); rows.Controls.Add(row);
        }
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 55, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
        var save = new RoundedButton { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true, ForeColor = Color.Black, BackColor = Color.DarkOrange };
        var cancel = new RoundedButton { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, ForeColor = Color.Black, BackColor = Color.WhiteSmoke };
        buttons.Controls.AddRange([save, cancel]); Controls.AddRange([rows, buttons]); AcceptButton = save; CancelButton = cancel;
    }
}

internal sealed class SpeedPercentInput : RoundedNumber
{
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        if (!Enabled || !ClientRectangle.Contains(PointToClient(Cursor.Position))) return;
        ApplyWheel(e.Delta, (ModifierKeys & Keys.Shift) != 0);
    }
    internal void ApplyWheel(int delta, bool fine)
    { Value = Math.Clamp(Value + delta / 120 * (fine ? 1 : 5), Minimum, Maximum); }
}