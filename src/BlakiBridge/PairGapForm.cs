namespace BlakiBridge;

public sealed class PairGapForm : ThemedForm
{
    private readonly RoundedNumber distance = new() { Minimum = 0, Maximum = 2000, DecimalPlaces = 1, Width = 110, Font = new Font("Segoe UI", 13), TextAlign = HorizontalAlignment.Center };
    public decimal Distance => distance.Value;
    internal void SetDistance(decimal value)=>distance.Value=value;
    internal readonly GapDiagram Diagram;
    internal readonly BackButton Back = new();
    internal FlowLayoutPanel ActionBar {get;private set;} = null!;
    internal readonly TableLayoutPanel Entry;
    public PairGapForm(Transition map, IReadOnlyList<MonitorModel> monitors, decimal? current, string action = "Save", string? progress = null)
    {
        Text = "Physical gap"; ClientSize = new Size(760, 470); MinimumSize = new Size(740, 500);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 11);
        BackColor = AppTheme.Background; ForeColor = Color.White;
        var intro = new SmoothLabel { Dock = DockStyle.Top, Height = 96, BackColor = AppTheme.Surface, Padding = new Padding(34, 12, 34, 8), Text = (progress is null ? "Distance between screens" : progress) + "\nMeasure from one lit image edge to the other. Include both bezels and the space between the cases." };
        bool vertical = map.Side is Edge.Top or Edge.Bottom;
        bool reverse = map.Side is Edge.Left or Edge.Top;
        Diagram = new GapDiagram(monitors, reverse ? map.B : map.A, reverse ? map.A : map.B, vertical) { Dock = DockStyle.Fill };
        Entry = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 56, ColumnCount = 1, RowCount = 1 };
        var entry = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.None };
        distance.Value = Math.Clamp(current ?? 0, 0, 2000);
        entry.Controls.Add(new SmoothLabel { Text = "Screen-to-screen distance:", AutoSize = true, Margin = new Padding(3, 7, 8, 3) }); entry.Controls.Add(distance);
        entry.Controls.Add(new SmoothLabel { Text = "mm", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }); Entry.Controls.Add(entry);
        var note = new SmoothLabel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(22, 4, 22, 0), Text = "Diagram and bezels are schematic. Accurate screen sizes improve travel precision." };
        var buttons = ActionBar = new LayoutRow { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(12), FlowDirection = FlowDirection.LeftToRight };
        var save = new RoundedButton { Text = action, AutoSize = true, BackColor = Color.DarkOrange, ForeColor = Color.Black };
        var cancel = new RoundedButton { Text = "Cancel", AutoSize = true, ForeColor = Color.WhiteSmoke, BackColor = AppTheme.Surface };
        save.Click+=(_,_)=>{DialogResult=DialogResult.OK;Close();};cancel.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};
        Back.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};
        var navigation=new FlowLayoutPanel{Dock=DockStyle.Top,Height=52,Padding=new Padding(22,8,0,0),WrapContents=false};
        Back.Height=44;Back.Margin=new Padding(0,0,8,0);
        navigation.Controls.AddRange([Back,new PageTabButton{Text=UiText.T("Physical gap"),Selected=true,AutoSize=true,Margin=Padding.Empty}]);
        buttons.Controls.AddRange([cancel, save]); Controls.AddRange([Diagram, Entry, note, buttons, intro,navigation]); AcceptButton = save; CancelButton = cancel;
        UiText.Apply(this);
    }
}

internal sealed class GapDiagram : Control
{
    private readonly IReadOnlyList<MonitorModel> monitors;
    private readonly string first,second;
    private readonly bool vertical;
    internal GapDiagram(IReadOnlyList<MonitorModel> monitors,string first,string second,bool vertical)
    {
        this.monitors=monitors;this.first=first;this.second=second;this.vertical=vertical;
        DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);
    }
    internal bool Vertical => vertical;
    internal Rectangle FirstImage { get; private set; }
    internal Rectangle SecondImage { get; private set; }
    internal Point Start { get; private set; }
    internal Point End { get; private set; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var area = Rectangle.Inflate(ClientRectangle, -24, -8);
        int gap = 34;
        var a = vertical ? new Rectangle(area.X, area.Y, area.Width, (area.Height - gap) / 2) : new Rectangle(area.X, area.Y, (area.Width - gap) / 2, area.Height);
        var b = vertical ? new Rectangle(area.X, a.Bottom + gap, area.Width, a.Height) : new Rectangle(a.Right + gap, area.Y, a.Width, area.Height);
        Rectangle Draw(string id, Rectangle slot)
        {
            var monitor = monitors.First(m => m.DeviceName == id);
            float scale = Math.Min((slot.Width - 24f) / monitor.Bounds.Width, (slot.Height - 24f) / monitor.Bounds.Height);
            var bounds = Rectangle.Round(new RectangleF(slot.X + (slot.Width - monitor.Bounds.Width * scale) / 2, slot.Y + (slot.Height - monitor.Bounds.Height * scale) / 2, monitor.Bounds.Width * scale, monitor.Bounds.Height * scale));
            var frame = Rectangle.Inflate(bounds, 10, 10);
            using var bezel = new SolidBrush(Color.FromArgb(11,11,11));
            e.Graphics.FillRectangle(bezel, frame); using(var framePen=new Pen(Color.FromArgb(91,91,91)))e.Graphics.DrawRectangle(framePen,frame);
            AppTheme.ScreenFill(e.Graphics,bounds,monitor.Primary,this);
            AppText.DrawText(e.Graphics, $"{UiText.T("Display")} {monitors.ToList().IndexOf(monitor) + 1}\n{monitor.Bounds.Width} × {monitor.Bounds.Height}", Font, bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return bounds;
        }
        FirstImage = Draw(first, a); SecondImage = Draw(second, b);
        Start = vertical ? new Point(FirstImage.Right - 16, FirstImage.Bottom) : new Point(FirstImage.Right, FirstImage.Top + FirstImage.Height / 2);
        End = vertical ? new Point(Start.X, SecondImage.Top) : new Point(SecondImage.Left, Start.Y);
        // Extension lines anchor the dimension to the lit image, never the outer bezel.
        using var pen = new Pen(AppTheme.Measurement, 2*DeviceDpi/96f);
        if (vertical)
        {
            e.Graphics.DrawLine(pen, SecondImage.Right, SecondImage.Top, End.X, End.Y);
            e.Graphics.DrawLine(pen, Start.X - 6, Start.Y, Start.X + 6, Start.Y);
            e.Graphics.DrawLine(pen, End.X - 6, End.Y, End.X + 6, End.Y);
        }
        else
        {
            e.Graphics.DrawLine(pen, SecondImage.Left, SecondImage.Top + SecondImage.Height / 2, End.X, End.Y);
            e.Graphics.DrawLine(pen, Start.X, Start.Y - 6, Start.X, Start.Y + 6);
            e.Graphics.DrawLine(pen, End.X, End.Y - 6, End.X, End.Y + 6);
        }
        AppTheme.Dimension(e.Graphics,Start,End,DeviceDpi/96f);
    }
}
