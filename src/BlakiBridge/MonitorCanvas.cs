using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace BlakiBridge;

public sealed class MonitorCanvas : Control
{
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<MonitorModel> Monitors { get; set; } = [];
    public Dictionary<string, Point> Positions { get; } = new();
    public Dictionary<string, float> ManualWidths { get; } = new();
    public Dictionary<string, DisplayMeasurements> Measurements { get; } = new();
    public Dictionary<string, PointF> DiagramPositions { get; } = new();
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AutoPhysicalSize { get; set; } = true;
    public event EventHandler? AppearanceChanged;
    public event Action<Transition>? GapSelected;
    internal readonly List<(Transition Map, Rectangle Badge, Point A, Point B)> GapBadges = new();
    internal readonly List<Rectangle> GapArrowBounds = new();
    internal readonly Dictionary<int, Rectangle> CaseBounds = new();
    private IReadOnlyList<Transition> gapMaps = [];
    private Dictionary<string, GapSettings> gapValues = new();
    private bool gapsEnabled;
    private Transition? hoveredGap;
    internal void ConfigureGaps(IReadOnlyList<Transition> maps, Dictionary<string, GapSettings> values, bool enabled)
    { gapMaps = maps; gapValues = values; gapsEnabled = enabled; Invalidate(); }
    internal Transition? GapAt(Point p) => null;
    internal List<(Rectangle Bounds, Size TextSize, string Text)> LabelMeasurements { get; } = new();
    private int selected = -1;
    private bool resizing;
    private float widthStart;
    private Dictionary<string, Point>? savedPositions;
    private Dictionary<string, PointF>? savedDiagram;
    private Dictionary<string, float>? savedWidths;
    internal string PaintedLanguage { get; private set; } = "";
    private PointF diagramStart;
    public SizeF DisplaySize(MonitorModel m)
    {
        return DisplaySizing.Resolve(m, ManualWidths, Measurements);
    }
    internal RectangleF DiagramBounds(MonitorModel m) => new(DiagramPosition(m),DisplaySize(m));
    private PointF DiagramPosition(MonitorModel m)
    {
        if (DiagramPositions.TryGetValue(m.DeviceName, out var stored) && float.IsFinite(stored.X) && float.IsFinite(stored.Y)) return stored;
        var primary = Monitors.FirstOrDefault(n => n.Primary) ?? Monitors[0];
        var origin = Positions.GetValueOrDefault(primary.DeviceName, primary.Bounds.Location);
        var p = Positions.GetValueOrDefault(m.DeviceName, m.Bounds.Location);
        var reference = DisplaySize(primary); var size = DisplaySize(m);
        float x = (p.X - origin.X) * reference.Width / primary.Bounds.Width;
        float y = (p.Y - origin.Y) * reference.Height / primary.Bounds.Height;
        if (p.X + m.Bounds.Width <= origin.X + 100) x = -size.Width + (p.X + m.Bounds.Width - origin.X) * reference.Width / primary.Bounds.Width;
        if (p.Y + m.Bounds.Height <= origin.Y + 100) y = -size.Height + (p.Y + m.Bounds.Height - origin.Y) * reference.Height / primary.Bounds.Height;
        return new PointF(x, y);
    }
    public event EventHandler? LayoutChanged;
    public event Action<int>? DisplayClicked;
    public event Action<int>? DisplaySelected;
    public event Action? MoveRejected;
    internal int SelectedIndex => selected;
    public event Action<int>? DisplayDoubleClicked;
    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (GapAt(e.Location) is not null) return;
        if (e.Button == MouseButtons.Left)
            foreach (var pair in _rects.Reverse()) if (pair.Value.Contains(e.Location)) { _dragIndex = -1; Capture = false; DisplayDoubleClicked?.Invoke(pair.Key); break; }
    }
    private Point mouseStart, positionStart;
    private float dragScale, viewScale, viewX, viewY;
    private bool moved;
    private int _dragIndex = -1;
    private readonly Dictionary<int, Rectangle> _rects = new();

    public MonitorCanvas()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.Background;
        SetStyle(ControlStyles.ResizeRedraw, true);
        AllowDrop = true;
        DragEnter += (_, e) => e.Effect = e.Data?.GetData(DataFormats.Text) as string == "BlakiBridge.GapTool" ? DragDropEffects.Copy : DragDropEffects.None;
        DragOver += (_, e) =>
        {
            hoveredGap = GapAt(PointToClient(new Point(e.X, e.Y)));
            e.Effect = hoveredGap is not null && e.Data?.GetData(DataFormats.Text) as string == "BlakiBridge.GapTool" ? DragDropEffects.Copy : DragDropEffects.None;
            Invalidate();
        };
        DragLeave += (_, _) => { hoveredGap = null; Invalidate(); };
        DragDrop += (_, e) =>
        {
            var pair = GapAt(PointToClient(new Point(e.X, e.Y))); hoveredGap = null; Invalidate();
            if (pair is not null && e.Data?.GetData(DataFormats.Text) as string == "BlakiBridge.GapTool") BeginInvoke(() => GapSelected?.Invoke(pair));
        };
    }

    public void ResetFromWindows()
    {
        Positions.Clear();
        foreach (var m in Monitors) Positions[m.DeviceName] = new Point(m.Bounds.X, m.Bounds.Y);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Background);
        PaintedLanguage = UiText.Language;
        float dpi=DeviceDpi/96f;
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int grid = Math.Max(24, (int)(28 * DeviceDpi / 96f));
        int dot = Math.Max(2, (int)(2 * DeviceDpi / 96f));
        float outlineWidth=Math.Max(1,(float)Math.Round(dpi));
        using var viewport=AppTheme.Round(new RectangleF(outlineWidth/2,outlineWidth/2,Math.Max(1,Width-outlineWidth),Math.Max(1,Height-outlineWidth)),6*dpi);
        var canvasState=e.Graphics.Save();
        e.Graphics.SetClip(viewport);
        using (var dots = new SolidBrush(Color.FromArgb(46,46,46)))
            for (int y = grid / 2; y < Height; y += grid)
                for (int x = grid / 2; x < Width; x += grid) e.Graphics.FillEllipse(dots, x, y, dot, dot);
        if (Monitors.Count == 0) { e.Graphics.Restore(canvasState); return; }
        var positions = Monitors.Select(DiagramPosition).ToArray();
        var physical=Monitors.Select((m,i)=>{var size=DisplaySize(m);var v=Measurements.GetValueOrDefault(m.DeviceName);return RectangleF.FromLTRB(positions[i].X-(float)(v?.Left??0),positions[i].Y-(float)(v?.Top??0),positions[i].X+size.Width+(float)(v?.Right??0),positions[i].Y+size.Height+(float)(v?.Bottom??0));}).ToArray();
        float minX=physical.Min(p=>p.Left),minY=physical.Min(p=>p.Top),maxX=physical.Max(p=>p.Right),maxY=physical.Max(p=>p.Bottom);
        float margin=22*dpi;
        float scale=Math.Max(.001f,Math.Min((Width-margin*2)/Math.Max(1,maxX-minX),(Height-margin*2)/Math.Max(1,maxY-minY)));
        float ox=(Width-(maxX-minX)*scale)/2,oy=(Height-(maxY-minY)*scale)/2;
        if (_dragIndex < 0) { viewScale = scale; viewX = ox - minX * scale; viewY = oy - minY * scale; }
        scale = viewScale;
        _rects.Clear();
        CaseBounds.Clear();
        LabelMeasurements.Clear();
        for (int i = 0; i < Monitors.Count; i++) LabelMeasurements.Add(default);
        // Paint all cases first so a neighbouring case never covers a lit image.
        for (int i = 0; i < Monitors.Count; i++)
        {
            var m = Monitors[i]; var position = positions[i]; var size = DisplaySize(m);
            var lit = Rectangle.Round(new RectangleF(viewX + position.X * scale, viewY + position.Y * scale, size.Width * scale, size.Height * scale));
            var measured = Measurements.GetValueOrDefault(m.DeviceName);
            int Frame(decimal? mm) => Math.Max(3, (int)Math.Round((double)(mm ?? 0) * scale));
            int left = Frame(measured?.Left), right = Frame(measured?.Right), top = Frame(measured?.Top), bottom = Frame(measured?.Bottom);
            var frame = Rectangle.FromLTRB(lit.Left - left, lit.Top - top, lit.Right + right, lit.Bottom + bottom);
            CaseBounds[i] = frame;
            using var bezel = new SolidBrush(Color.FromArgb(10,10,10)); e.Graphics.FillRectangle(bezel, frame);
            using var border = new Pen(i == selected ? AppTheme.Accent : Color.FromArgb(107,107,107), Math.Max(1,(float)Math.Round(dpi)) * (i == selected ? 2 : 1));
            e.Graphics.DrawRectangle(border, frame);
        }
        // Smaller displays remain reachable even while a larger display crosses them.
        foreach (int i in Enumerable.Range(0, Monitors.Count).OrderByDescending(i => DisplaySize(Monitors[i]).Width * DisplaySize(Monitors[i]).Height))
        {
            var m = Monitors[i]; var p = positions[i];
            var size = DisplaySize(m);
            var r = Rectangle.Round(new RectangleF(viewX + p.X * scale, viewY + p.Y * scale, size.Width * scale, size.Height * scale));
            _rects[i] = r;
            AppTheme.ScreenFill(e.Graphics,r,m.Primary,this);
            using var pen=new Pen(Color.FromArgb(180,180,180),Math.Max(1,dpi));
            var title = $"{m.Bounds.Width} × {m.Bounds.Height}";
            int numberSize = Math.Min((int)(26*dpi), Math.Max((int)(12*dpi), r.Height - (int)(8*dpi)));
            var numberBounds = new Rectangle(r.Left + 5, r.Top + 5, numberSize, numberSize);
            using (var badgeFill = new SolidBrush(m.Primary ? AppTheme.Accent : Color.FromArgb(100,100,100)))
            using(var badgePath=AppTheme.Round(numberBounds,3*dpi)) e.Graphics.FillPath(badgeFill,badgePath);
            using (var numberFont = new Font("Segoe UI",numberSize*.56f,FontStyle.Bold,GraphicsUnit.Pixel))
                AppText.DrawText(e.Graphics, (i + 1).ToString(), numberFont, numberBounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            var textArea = r.Height < 75 ? Rectangle.FromLTRB(numberBounds.Right + 5, r.Top, r.Right - 5, r.Bottom) : Rectangle.Inflate(r, -5, -5);
            if (r.Height > 105 && !ManualWidths.ContainsKey(m.DeviceName)) title += Measurements.GetValueOrDefault(m.DeviceName) is { Valid: true } measured ? $"\n{measured.Width:0.#} × {measured.Height:0.#} mm" : "";
            float fontSize = 13*dpi;
            Size textSize;
            while (true)
            {
                using var candidate = new Font("Segoe UI", fontSize, FontStyle.Bold);
                textSize = AppText.MeasureText(title,candidate);
                if ((textSize.Width <= textArea.Width - 4 && textSize.Height <= textArea.Height - 4) || fontSize <= 6*dpi) break;
                fontSize -= .5f;
            }
            LabelMeasurements[i] = (r, textSize, title);
            using var titleFont = new Font("Segoe UI", fontSize, FontStyle.Bold);
            AppText.DrawText(e.Graphics, title, titleFont, textArea, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            e.Graphics.DrawLine(pen, r.Right - 18, r.Bottom - 4, r.Right - 4, r.Bottom - 18); e.Graphics.DrawLine(pen, r.Right - 10, r.Bottom - 4, r.Right - 4, r.Bottom - 10);
        }
        GapBadges.Clear();
        GapArrowBounds.Clear();
        e.Graphics.Restore(canvasState);
        using var viewportBorder = new Pen(AppTheme.Border, outlineWidth);
        e.Graphics.DrawPath(viewportBorder, viewport);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        savedPositions = new(Positions); savedDiagram = new(DiagramPositions); savedWidths = new(ManualWidths);
        foreach (var pair in _rects.Reverse()) if (pair.Value.Contains(e.Location))
        { _dragIndex = pair.Key; selected = pair.Key; mouseStart = e.Location; positionStart = Positions.GetValueOrDefault(Monitors[_dragIndex].DeviceName, Monitors[_dragIndex].Bounds.Location); diagramStart = DiagramPosition(Monitors[_dragIndex]); widthStart = DisplaySize(Monitors[_dragIndex]).Width; resizing = e.X >= pair.Value.Right - 24 && e.Y >= pair.Value.Bottom - 24; dragScale = viewScale; moved = false; Capture = true; Invalidate(); DisplaySelected?.Invoke(selected); break; }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragIndex < 0 || !Capture)
        {
            var hit = GapBadges.FirstOrDefault(b => b.Badge.Contains(e.Location)).Map;
            if (hit != hoveredGap) { hoveredGap = hit; Invalidate(); }
            return;
        }
        int dxPixels = e.X - mouseStart.X;
        int dyPixels = e.Y - mouseStart.Y;
        if (Math.Abs(dxPixels) + Math.Abs(dyPixels) < 6 && !moved) return;
        moved = true;
        var m = Monitors[_dragIndex];
        if (resizing) ManualWidths[m.DeviceName] = Math.Clamp(widthStart + (dxPixels + dyPixels * m.Bounds.Height / (float)m.Bounds.Width) / (1 + MathF.Pow(m.Bounds.Height / (float)m.Bounds.Width, 2)) / dragScale, 40, 4000);
        else
        {
            DiagramPositions[m.DeviceName] = new PointF(diagramStart.X + dxPixels / dragScale, diagramStart.Y + dyPixels / dragScale);
            var reference = Monitors.FirstOrDefault(n => n.Primary) ?? Monitors[0];
            float ratio = reference.Bounds.Width / DisplaySize(reference).Width;
            Positions[m.DeviceName] = new Point(positionStart.X + (int)(dxPixels / dragScale * ratio), positionStart.Y + (int)(dyPixels / dragScale * ratio));
        }
        Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (hoveredGap is not null) { hoveredGap = null; Invalidate(); } }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        int clicked = _dragIndex;
        if (clicked >= 0 && moved && HasOverlap(clicked))
        {
            Positions.Clear(); foreach (var pair in savedPositions!) Positions.Add(pair.Key, pair.Value);
            DiagramPositions.Clear(); foreach (var pair in savedDiagram!) DiagramPositions.Add(pair.Key, pair.Value);
            ManualWidths.Clear(); foreach (var pair in savedWidths!) ManualWidths.Add(pair.Key, pair.Value);
            _dragIndex = -1; Capture = false; Invalidate(); MoveRejected?.Invoke(); return;
        }
        if (clicked >= 0 && moved) { if (resizing) AppearanceChanged?.Invoke(this, EventArgs.Empty); else LayoutChanged?.Invoke(this, EventArgs.Empty); }
        _dragIndex = -1; Capture = false; Invalidate();
        if (clicked >= 0 && !moved) DisplayClicked?.Invoke(clicked);
    }
    internal void CancelInteraction()
    {
        if (_dragIndex >= 0)
        {
            if (savedPositions is not null) { Positions.Clear(); foreach (var pair in savedPositions) Positions.Add(pair.Key, pair.Value); }
            if (savedDiagram is not null) { DiagramPositions.Clear(); foreach (var pair in savedDiagram) DiagramPositions.Add(pair.Key, pair.Value); }
            if (savedWidths is not null) { ManualWidths.Clear(); foreach (var pair in savedWidths) ManualWidths.Add(pair.Key, pair.Value); }
        }
        _dragIndex = -1; moved = false; Capture = false; Invalidate();
    }
    internal bool HasOverlap(int index)
    {
        var bounds = new RectangleF(DiagramPosition(Monitors[index]), DisplaySize(Monitors[index]));
        return Monitors.Where((_, i) => i != index).Any(m =>
        {
            var intersection = RectangleF.Intersect(bounds, new RectangleF(DiagramPosition(m), DisplaySize(m)));
            return intersection.Width > .01f && intersection.Height > .01f;
        });
    }
}
