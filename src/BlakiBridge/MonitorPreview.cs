using System.Drawing.Drawing2D;

namespace BlakiBridge;

public sealed class MonitorPreview : Control
{
    private DisplayMeasurements dimensions = new() { Width = 500, Height = 300 };
    private int selected = -1;
    internal int Selected => selected;
    internal RectangleF ImageBounds, CaseBounds, HighlightBounds;
    internal string Caption = "";
    internal string MonitorName = "";
    internal bool ShowParameterCaption = true;
    private static readonly string[] Names = ["Active image width", "Active image height", "Left bezel", "Right bezel", "Top bezel", "Bottom bezel"];
    public MonitorPreview() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); }
    internal void SetMeasurements(DisplayMeasurements value) { dimensions = value; Invalidate(); }
    internal void SelectParameter(int index) { selected = index; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Background);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        float w = (float)dimensions.Width, h = (float)dimensions.Height;
        float l = (float)dimensions.Left, r = (float)dimensions.Right, t = (float)dimensions.Top, b = (float)dimensions.Bottom;
        float scale = Math.Max(.001f, Math.Min((Width - 100f) / (w + l + r), (Height - 130f) / (h + t + b)));
        float outerW = (w + l + r) * scale, outerH = (h + t + b) * scale;
        CaseBounds = new RectangleF((Width - outerW) / 2, (Height - outerH) / 2 - 10, outerW, outerH);
        ImageBounds = new RectangleF(CaseBounds.Left + l * scale, CaseBounds.Top + t * scale, w * scale, h * scale);
        using var bezel = new SolidBrush(Color.FromArgb(116,116,116));
        using var screen = new SolidBrush(Color.FromArgb(48,48,48));
        using var outline = new Pen(Color.Silver, 1);
        g.FillRectangle(bezel, CaseBounds); AppTheme.ScreenFill(g,ImageBounds,owner:this);
        g.DrawRectangle(outline, CaseBounds.X, CaseBounds.Y, CaseBounds.Width, CaseBounds.Height);
        HighlightBounds = selected switch
        {
            2 => new RectangleF(CaseBounds.Left, CaseBounds.Top, l * scale, CaseBounds.Height),
            3 => new RectangleF(ImageBounds.Right, CaseBounds.Top, r * scale, CaseBounds.Height),
            4 => new RectangleF(CaseBounds.Left, CaseBounds.Top, CaseBounds.Width, t * scale),
            5 => new RectangleF(CaseBounds.Left, ImageBounds.Bottom, CaseBounds.Width, b * scale),
            _ => ImageBounds
        };
        if (selected >= 0)
        {
            var measuredBounds = HighlightBounds;
            // A zero-width bezel still has a location. Highlight that edge without
            // changing the measurement or pretending the bezel is physically thick.
            if (selected is 2 or 3 && HighlightBounds.Width < 1)
                HighlightBounds = new RectangleF(HighlightBounds.X - 2, HighlightBounds.Y, 4, HighlightBounds.Height);
            if (selected is 4 or 5 && HighlightBounds.Height < 1)
                HighlightBounds = new RectangleF(HighlightBounds.X, HighlightBounds.Y - 2, HighlightBounds.Width, 4);
            using var highlight = new SolidBrush(Color.FromArgb(75, 255, 151, 30));
            g.FillRectangle(highlight, HighlightBounds);
            using var edge = new Pen(Color.DarkOrange, 3);
            g.DrawRectangle(edge, HighlightBounds.X, HighlightBounds.Y, HighlightBounds.Width, HighlightBounds.Height);
            float dpi=DeviceDpi/96f,offset=22*dpi;
            if(selected==0)AppTheme.Dimension(g,new(ImageBounds.Left,ImageBounds.Bottom+offset),new(ImageBounds.Right,ImageBounds.Bottom+offset),dpi);
            else if(selected==1)AppTheme.Dimension(g,new(ImageBounds.Left-offset,ImageBounds.Top),new(ImageBounds.Left-offset,ImageBounds.Bottom),dpi);
            else if(selected is 2 or 3 && measuredBounds.Width>0)AppTheme.Dimension(g,new(measuredBounds.Left,CaseBounds.Bottom+offset),new(measuredBounds.Right,CaseBounds.Bottom+offset),dpi);
            else if(selected is 4 or 5 && measuredBounds.Height>0)AppTheme.Dimension(g,new(CaseBounds.Right+offset,measuredBounds.Top),new(CaseBounds.Right+offset,measuredBounds.Bottom),dpi);
        }
        decimal[] numbers = [dimensions.Width, dimensions.Height, dimensions.Left, dimensions.Right, dimensions.Top, dimensions.Bottom];
        Caption = selected < 0 ? UiText.Pick("Hover over a parameter", "Užveskite pelę ant parametro", "Наведите на параметр") : $"{UiText.T(Names[selected])} · {numbers[selected]:0.#} mm";
        if (ShowParameterCaption) AppText.DrawText(g, Caption, Font, new Rectangle(0, Height - 40, Width, 36), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        AppText.DrawText(g, MonitorName, Font, Rectangle.Round(ImageBounds), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
