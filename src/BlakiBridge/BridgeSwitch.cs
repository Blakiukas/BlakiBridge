using System.Drawing.Drawing2D;

namespace BlakiBridge;

internal sealed class BridgeSwitch : CheckBox
{
    private bool hover;
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    internal bool TrailingTrack;
    internal bool WindowsMark;
    public BridgeSwitch()
    {
        Cursor=Cursors.Hand;FlatStyle = FlatStyle.Flat;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        CheckedChanged += (_, _) => Invalidate();
        AutoSize = true;
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = AppText.MeasureText(Text, Font);
        return new Size(text.Width + (int)((WindowsMark ? 98 : 58) * DeviceDpi / 96f), Math.Max(text.Height, (int)(32 * DeviceDpi / 96f)));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Background);
        e.Graphics.SetClip(ClientRectangle);
        float s = DeviceDpi / 96f;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new RectangleF(TrailingTrack ? Width-44*s : 0, (Height - 24 * s) / 2, 44 * s, 24 * s);
        using var path = new GraphicsPath();
        path.AddArc(track.X, track.Y, track.Height, track.Height, 90, 180);
        path.AddArc(track.Right - track.Height, track.Y, track.Height, track.Height, 270, 180); path.CloseFigure();
        using var fill = new SolidBrush(Checked ? AppTheme.Accent : Color.FromArgb(80,80,80));
        e.Graphics.FillPath(fill, path);if(hover&&Enabled){using var outline=new Pen(Color.FromArgb(190,190,190),2*s);e.Graphics.DrawPath(outline,path);}
        e.Graphics.FillEllipse(Brushes.WhiteSmoke, track.X + (Checked ? 23 : 3) * s, track.Y + 3 * s, 18 * s, 18 * s);
        int textLeft=(int)((WindowsMark?36:TrailingTrack?0:56)*s);
        var text = new Rectangle(textLeft, 0, Math.Max(0, Width-textLeft-(TrailingTrack?(int)(56*s):0)),Height);
        if(WindowsMark)
        {
            using var mark=new SolidBrush(AppTheme.Accent); float x=2*s,y=Height/2f;
            e.Graphics.FillPolygon(mark,[new PointF(x,y-9*s),new PointF(x+10*s,y-10*s),new PointF(x+10*s,y-s),new PointF(x,y-s)]);
            e.Graphics.FillPolygon(mark,[new PointF(x+12*s,y-10*s),new PointF(x+24*s,y-12*s),new PointF(x+24*s,y-s),new PointF(x+12*s,y-s)]);
            e.Graphics.FillPolygon(mark,[new PointF(x,y+s),new PointF(x+10*s,y+s),new PointF(x+10*s,y+10*s),new PointF(x,y+9*s)]);
            e.Graphics.FillPolygon(mark,[new PointF(x+12*s,y+s),new PointF(x+24*s,y+s),new PointF(x+24*s,y+12*s),new PointF(x+12*s,y+10*s)]);
        }
        AppText.DrawText(e.Graphics, Text, Font, text, Enabled ? ForeColor : SystemColors.GrayText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, text);
    }
}
