namespace BlakiBridge;

public sealed class AutoAlignment : IDisposable
{
    private readonly Form owner;
    private readonly BridgeConfig config;
    private readonly Action<string> completed;
    private GuidedMarker? a, b;
    private Transition? map;
    private bool finishing;
    public AutoAlignment(Form owner, BridgeConfig config, Action<string> completed)
    { this.owner = owner; this.config = config; this.completed = completed; }

    public static (int Other, Edge Side) FindNeighbor(int index, IReadOnlyList<MonitorModel> monitors, Dictionary<string, Point> positions)
    {
        Rectangle Rect(int i) => new(positions.GetValueOrDefault(monitors[i].DeviceName, monitors[i].Bounds.Location), monitors[i].Bounds.Size);
        var source = Rect(index);
        var choices = new List<(double Score, int Other, Edge Side)>();
        for (int i = 0; i < monitors.Count; i++)
        {
            if (i == index) continue;
            var target = Rect(i);
            int overlapY = Math.Min(source.Bottom, target.Bottom) - Math.Max(source.Top, target.Top);
            int overlapX = Math.Min(source.Right, target.Right) - Math.Max(source.Left, target.Left);
            if (overlapY > 0)
            {
                choices.Add((Math.Abs(source.Left - target.Right), i, Edge.Left));
                choices.Add((Math.Abs(source.Right - target.Left), i, Edge.Right));
            }
            if (overlapX > 0)
            {
                choices.Add((Math.Abs(source.Top - target.Bottom), i, Edge.Top));
                choices.Add((Math.Abs(source.Bottom - target.Top), i, Edge.Bottom));
            }
        }
        if (choices.Count == 0) return (-1, Edge.Left);
        var best = choices.OrderBy(c => c.Score).ThenBy(c => c.Other).First();
        return (best.Other, best.Side);
    }

    public void Start(int index, IReadOnlyList<MonitorModel> monitors, Dictionary<string, Point> positions)
    {
        var (other, side) = FindNeighbor(index, monitors, positions);
        if (other < 0) { completed("Move this display beside another display, then click it."); return; }
        var first = monitors[index]; var second = monitors[other];
        if (new[] { first, second }.Any(m => !Screen.AllScreens.Any(s => s.DeviceName == m.DeviceName && s.Bounds == m.Bounds)))
        { completed("Displays changed. Restart BlakiBridge to refresh them."); return; }
        map = new Transition { A = first.DeviceName, B = second.DeviceName, Side = side, SizeA = first.Bounds.Size, SizeB = second.Bounds.Size };
        a = new GuidedMarker(first.Bounds, index + 1, other + 1, side);
        b = new GuidedMarker(second.Bounds, other + 1, index + 1, Transition.Opposite(side));
        a.Next += Advance; b.Next += Advance;
        a.Cancel += Cancel; b.Cancel += Cancel;
        a.AnchorRequested += () => SetAnchor(true);
        b.AnchorRequested += () => SetAnchor(false);
        SetAnchor(true);
        // Modeless overlays stay alive while the main window remains in its message loop.
        a.Show(owner); b.Show(owner);
        a.Bounds = first.Bounds; b.Bounds = second.Bounds;
        a.Activate();
    }

    private void SetAnchor(bool first)
    {
        a?.SetFixed(first); b?.SetFixed(!first);
    }

    private void Advance()
    {
        if (a is null || b is null || map is null || finishing) return;
        if (a.Step == 0)
        {
            map.StartA = a.Coordinate; map.StartB = b.Coordinate;
            bool fixedFirst = a.IsFixed;
            a.SecondStep(); b.SecondStep(); SetAnchor(fixedFirst); return;
        }
        map.EndA = a.Coordinate; map.EndB = b.Coordinate;
        if (!map.Valid) return;
        var previous = config.Transitions.ToList();
        config.Transitions.RemoveAll(t => (t.A == map.A && t.B == map.B && t.Side == map.Side) || (t.A == map.B && t.B == map.A && t.Side == Transition.Opposite(map.Side)));
        config.Transitions.Add(map);
        try { config.Save(); }
        catch (Exception ex) { config.Transitions = previous; MessageBox.Show(a, ex.Message, "Could not save alignment"); return; }
        Dispose(); completed("Alignment saved. Click Try it to test the mouse crossing.");
    }
    private void Cancel() { Dispose(); completed("Alignment cancelled. Click a display to try again."); }
    public void Dispose()
    {
        finishing = true;
        a?.Dispose(); b?.Dispose(); a = null; b = null;
        if (!owner.IsDisposed) owner.Activate();
    }
}

public sealed class GuidedMarker : Form
{
    private readonly Edge edge;
    private readonly int number, other;
    private readonly Label instruction = new() { Dock = DockStyle.Top, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button next = new() { Text = "Next", AutoSize = true, BackColor = Color.FromArgb(207, 112, 20), ForeColor = Color.White };
    private int coordinate, minimum;
    private bool dragging;
    private bool isFixed;
    public bool IsFixed => isFixed;
    public event Action? AnchorRequested;
    public int Step { get; private set; }
    public int Coordinate => coordinate;
    public event Action? Next;
    public event Action? Cancel;
    private int Length => Transition.Vertical(edge) ? ClientSize.Height : ClientSize.Width;
    public GuidedMarker(Rectangle bounds, int number, int other, Edge edge)
    {
        this.edge = edge; this.number = number; this.other = other;
        AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual; Bounds = bounds;
        TopMost = true; ShowInTaskbar = false; KeyPreview = true; DoubleBuffered = true;
        BackColor = Color.FromArgb(24,24,24); ForeColor = Color.White;
        Font = new Font("Segoe UI", 15);
        coordinate = Math.Max(0, Length / 4);
        var panel = new Panel { Size = new Size(Math.Min(760, bounds.Width - 280), 210), BackColor = BackColor };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Text = "Cancel", AutoSize = true, BackColor = Color.WhiteSmoke, ForeColor = Color.Black };
        var anchor = new Button { Text = "Use this screen's edge", AutoSize = true, BackColor = Color.WhiteSmoke, ForeColor = Color.Black };
        anchor.Click += (_, _) => AnchorRequested?.Invoke();
        next.Click += (_, _) => Next?.Invoke(); cancel.Click += (_, _) => Cancel?.Invoke();
        buttons.Controls.AddRange([next, anchor, cancel]); panel.Controls.AddRange([instruction, buttons]); Controls.Add(panel);
        void PositionPanel() => panel.Location = new Point((ClientSize.Width - panel.Width) / 2, (ClientSize.Height - panel.Height) / 2);
        Resize += (_, _) => PositionPanel(); PositionPanel(); UpdateInstruction();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Cancel?.Invoke();
            if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; Next?.Invoke(); }
            if (!isFixed && e.KeyCode is Keys.Up or Keys.Down or Keys.Left or Keys.Right)
            {
                int delta = e.KeyCode is Keys.Up or Keys.Left ? -1 : 1;
                coordinate = Math.Clamp(coordinate + delta * (e.Shift ? 10 : 1), minimum, Length - (Step == 0 ? 2 : 1));
                e.Handled = true; e.SuppressKeyPress = true; Invalidate();
            }
        };
        FormClosing += (_, e) => { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Cancel?.Invoke(); } };
    }
    private void UpdateInstruction()
    {
        string boundary = Transition.Vertical(edge) ? (Step == 0 ? "top" : "bottom") : (Step == 0 ? "left end" : "right end");
        instruction.MaximumSize = new Size(Parent is null ? Controls[0].Width : 600, 0);
        instruction.Text = isFixed
            ? $"Step {Step + 1} of 2 · Display {number}\nThis line is fixed to this screen's {boundary}.\nMove the line on display {other} to this height or position.\nIf they cannot meet, click 'Use this screen's edge' on display {other}."
            : $"Step {Step + 1} of 2 · Display {number}\nDrag this line to meet the fixed line on display {other}.\nArrow keys: fine adjustment.\nThen click {(Step == 0 ? "Next" : "Done")}.";
    }
    public void SetFixed(bool value)
    {
        isFixed = value;
        if (value) coordinate = Step == 0 ? 0 : Length - 1;
        UpdateInstruction(); Invalidate();
    }
    public void SecondStep()
    {
        Step = 1; minimum = coordinate + 1;
        coordinate = Math.Max(minimum, (Length - 1) * 3 / 4);
        next.Text = "Done"; SetFixed(isFixed);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Step == 0 ? Color.Orange : Color.DeepSkyBlue, 8);
        using var brush = new SolidBrush(pen.Color);
        if (Transition.Vertical(edge))
        {
            int x = edge == Edge.Left ? 0 : ClientSize.Width - 180;
            e.Graphics.DrawLine(pen, x, coordinate, x + 180, coordinate);
            e.Graphics.FillEllipse(brush, x + 64, Math.Clamp(coordinate - 20, 0, ClientSize.Height - 40), 40, 40);
        }
        else
        {
            int y = edge == Edge.Top ? 0 : ClientSize.Height - 180;
            e.Graphics.DrawLine(pen, coordinate, y, coordinate, y + 180);
            e.Graphics.FillEllipse(brush, Math.Clamp(coordinate - 20, 0, ClientSize.Width - 40), y + 64, 40, 40);
        }
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || isFixed) return;
        int p = Transition.Vertical(edge) ? e.Y : e.X;
        bool nearEdge = edge switch { Edge.Left => e.X <= 200, Edge.Right => e.X >= Width - 200, Edge.Top => e.Y <= 200, _ => e.Y >= Height - 200 };
        if (!nearEdge || Math.Abs(p - coordinate) > 35) return;
        dragging = true; Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!dragging) return;
        coordinate = Math.Clamp(Transition.Vertical(edge) ? e.Y : e.X, minimum, Length - (Step == 0 ? 2 : 1)); Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e) { dragging = false; Capture = false; }
}
