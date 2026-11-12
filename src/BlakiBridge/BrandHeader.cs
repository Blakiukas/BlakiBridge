using System.Drawing.Text;

namespace BlakiBridge;

internal sealed class BrandHeader : Control
{
    internal RectangleF MarkBounds { get; private set; }
    internal RectangleF WordBounds { get; private set; }
    internal BrandHeader()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        AccessibleName = "BlakiBridge";

    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        float scale = DeviceDpi / 96f;
        float side = Math.Min(48 * scale, Height - 8 * scale);
        MarkBounds = new RectangleF(22 * scale, (Height - side) / 2, side, side);
        BrandGlyph.Draw(e.Graphics,MarkBounds);
        using var font = new Font("Segoe UI", 24 * scale, FontStyle.Bold);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var first = e.Graphics.MeasureString("Blaki", font, PointF.Empty, format);
        var second = e.Graphics.MeasureString("Bridge", font, PointF.Empty, format);
        float x = MarkBounds.Right + 14 * scale;
        float y = (Height - Math.Max(first.Height, second.Height)) / 2;
        WordBounds = new RectangleF(x, y, first.Width + second.Width, Math.Max(first.Height, second.Height));
        e.Graphics.DrawString("Blaki", font, Brushes.WhiteSmoke, x, y, format);
        using var orange = new SolidBrush(Color.FromArgb(255, 139, 18));
        e.Graphics.DrawString("Bridge", font, orange, x + first.Width, y, format);
    }
}
