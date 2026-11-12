using System.Runtime.InteropServices;

namespace BlakiBridge;

internal sealed class LiveMarkers : IDisposable
{
    private readonly List<MarkerHandle> windows = [];
    public LiveMarkers(Form owner, List<Transition> maps, IReadOnlyList<MonitorModel> monitors, Action<bool> drag, Action save)
    {
        Color[] colors = [Color.Orange, Color.DeepSkyBlue, Color.LimeGreen, Color.Violet];
        for (int i = 0; i < maps.Count; i++)
        {
            var map = maps[i];
            for (int side = 0; side < 2; side++)
            {
                bool reverse = side == 1;
                var monitor = monitors.First(m => m.DeviceName == (reverse ? map.B : map.A));
                for (int endpoint = 0; endpoint < 2; endpoint++)
                {
                    bool end = endpoint == 1;
                    var marker = new MarkerHandle(monitor.Bounds, reverse ? Transition.Opposite(map.Side) : map.Side, colors[i % colors.Length], $"{monitors.ToList().FindIndex(m=>m.DeviceName==map.A)+1}–{monitors.ToList().FindIndex(m=>m.DeviceName==map.B)+1} {(end ? "B" : "A")}",
                        () => reverse ? (end ? map.EndB : map.StartB) : (end ? map.EndA : map.StartA),
                        value =>
                        {
                            int length = Transition.Vertical(map.Side) ? monitor.Bounds.Height : monitor.Bounds.Width;
                            int lower = reverse ? map.StartB : map.StartA;
                            int upper = reverse ? map.EndB : map.EndA;
                            value = Math.Clamp(value, end ? lower + 1 : 0, end ? length - 1 : upper - 1);
                            if (reverse) { if (end) map.EndB = value; else map.StartB = value; }
                            else { if (end) map.EndA = value; else map.StartA = value; }
                        }, drag, save, Math.Max(1f, monitor.DpiX / 96f));
                    windows.Add(marker); marker.Show(owner); marker.Place();
                }
            }
        }
    }
    public void Dispose() { foreach (var marker in windows) marker.Dispose(); windows.Clear(); }
}

internal sealed class MarkerHandle : Form
{
    private readonly Rectangle screen;
    private readonly Edge edge;
    private readonly Color color;
    private readonly string label;
    private readonly Func<int> get;
    private readonly Action<int> set;
    private readonly Action<bool> drag;
    private readonly Action save;
    private bool moving, clipped;
    private NativeRect prior;
    private readonly float scale;
    public MarkerHandle(Rectangle screen, Edge edge, Color color, string label, Func<int> get, Action<int> set, Action<bool> drag, Action save, float scale = 1)
    {
        Text = $"BlakiBridge · {label} · {edge}";
        this.scale = scale;
        this.screen = screen; this.edge = edge; this.color = color; this.label = label; this.get = get; this.set = set; this.drag = drag; this.save = save;
        AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false; TopMost = true; DoubleBuffered = true; BackColor = Color.FromArgb(31,31,31);
        Size = HandleSize();
        Cursor = Transition.Vertical(edge) ? Cursors.SizeNS : Cursors.SizeWE;
        Position();
        MouseCaptureChanged += (_, _) => { if (moving && !Capture) Finish(); };
    }
    protected override bool ShowWithoutActivation => true;
    public void Place()
    {
        Size = HandleSize();
        Position();
    }
    private Size HandleSize() => Transition.Vertical(edge) ? new Size((int)(160 * scale), (int)(64 * scale)) : new Size((int)(64 * scale), (int)(160 * scale));
    private void Position()
    {
        int p = get();
        Location = edge switch
        {
            Edge.Left => new Point(screen.Left, Math.Clamp(screen.Top + p - Height / 2, screen.Top, screen.Bottom - Height)),
            Edge.Right => new Point(screen.Right - Width, Math.Clamp(screen.Top + p - Height / 2, screen.Top, screen.Bottom - Height)),
            Edge.Top => new Point(Math.Clamp(screen.Left + p - Width / 2, screen.Left, screen.Right - Width), screen.Top),
            _ => new Point(Math.Clamp(screen.Left + p - Width / 2, screen.Left, screen.Right - Width), screen.Bottom - Height)
        };
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int p=Transition.Vertical(edge)?screen.Top+get()-Top:screen.Left+get()-Left;
        var tab=Transition.Vertical(edge)
            ?new Rectangle(edge==Edge.Left?(int)(35*scale):0,(Height-(int)(42*scale))/2,(int)(120*scale),(int)(42*scale))
            :new Rectangle((Width-(int)(48*scale))/2,edge==Edge.Top?(int)(35*scale):0,(int)(48*scale),(int)(110*scale));
        using var path=AppTheme.Round(Rectangle.Inflate(tab,-1,-1),6*scale);using var fill=new SolidBrush(AppTheme.Surface);using var border=new Pen(color,Math.Max(1,scale));
        e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(border,path);
        using var line=new Pen(color,2*scale);using var tip=new SolidBrush(color);float head=8*scale,half=4*scale;
        if(Transition.Vertical(edge))
        {
            float x=edge==Edge.Left?0:Width-1,dir=edge==Edge.Left?1:-1,to=edge==Edge.Left?tab.Left:tab.Right;
            e.Graphics.DrawLine(line,to,tab.Top+tab.Height/2f,to,p);e.Graphics.DrawLine(line,x,p,to,p);e.Graphics.FillPolygon(tip,[new PointF(x,p),new PointF(x+dir*head,p-half),new PointF(x+dir*head,p+half)]);
        }
        else
        {
            float y=edge==Edge.Top?0:Height-1,dir=edge==Edge.Top?1:-1,to=edge==Edge.Top?tab.Top:tab.Bottom;
            e.Graphics.DrawLine(line,tab.Left+tab.Width/2f,to,p,to);e.Graphics.DrawLine(line,p,y,p,to);e.Graphics.FillPolygon(tip,[new PointF(p,y),new PointF(p-half,y+dir*head),new PointF(p+half,y+dir*head)]);
        }
        using var font=new Font("Segoe UI",14*scale,FontStyle.Bold,GraphicsUnit.Pixel);
        AppText.DrawText(e.Graphics,label+(Transition.Vertical(edge)?" ↕":"\n↔"),font,tab,AppTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        drag(true); moving = true;
        if (GetClipCursor(out prior))
        {
            var bounds = new NativeRect { Left = screen.Left, Top = screen.Top, Right = screen.Right, Bottom = screen.Bottom };
            clipped = ClipCursor(ref bounds);
        }
        Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!moving) return;
        var cursor = Cursor.Position;
        int value = Transition.Vertical(edge) ? cursor.Y - screen.Top : cursor.X - screen.Left;
        int length = Transition.Vertical(edge) ? screen.Height : screen.Width;
        if (value < 8) value = 0; else if (value > length - 9) value = length - 1;
        set(value); Position();
    }
    protected override void OnMouseUp(MouseEventArgs e) => Finish();
    private void Finish()
    {
        if (!moving) return;
        moving = false;
        if (clipped) { ClipCursor(ref prior); clipped = false; }
        Capture = false; save(); drag(false);
    }
    protected override void Dispose(bool disposing) { if (disposing) Finish(); base.Dispose(disposing); }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetClipCursor(out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(ref NativeRect rect);
}
