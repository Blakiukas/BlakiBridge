using System.Drawing.Drawing2D;
namespace BlakiBridge;

internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    internal static readonly Color Background = Color.FromArgb(31,31,31);
    public TrayMenuRenderer() { RoundedEdges = false; }
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) => e.Graphics.Clear(Background);
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(75,75,75));
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width-1, e.ToolStrip.Height-1);
    }
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        using var fill = new SolidBrush(e.Item.Selected ? Color.FromArgb(49,49,49) : Background);
        e.Graphics.FillRectangle(fill, new Rectangle(Point.Empty, e.Item.Size));
        if (e.Item.Owner?.Items.IndexOf(e.Item) == 4)
        {
            using var pen = new Pen(Color.FromArgb(115,115,115));
            e.Graphics.DrawLine(pen, 32 * (e.ToolStrip?.DeviceDpi ?? 96) / 96, 0, e.Item.Width-8, 0);
        }
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.FromArgb(238,238,238); base.OnRenderItemText(e);
    }
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var r = e.ImageRectangle;
        var previous = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(255,132,0), 2f * (e.ToolStrip?.DeviceDpi ?? 96) / 96) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLines(pen, [new PointF(r.Left+r.Width*.23f,r.Top+r.Height*.53f),new PointF(r.Left+r.Width*.43f,r.Top+r.Height*.76f),new PointF(r.Left+r.Width*.83f,r.Top+r.Height*.17f)]);
        e.Graphics.SmoothingMode = previous;
    }
}