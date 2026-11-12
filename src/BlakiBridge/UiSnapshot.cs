namespace BlakiBridge;
// DrawToBitmap reverses native/user-painted sibling ordering. Composite the visible numeric readout last.
internal static class UiSnapshot
{
    private static IEnumerable<Control> Descendants(Control root)
    {foreach(Control child in root.Controls){if(!child.Visible)continue;yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    internal static void Paint(Control root,Bitmap bitmap)
    {
        root.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));
        using var graphics=Graphics.FromImage(bitmap);
        foreach(var child in Descendants(root).Where(c=>c.GetType().Name=="NumberReadout"))
        {
            using var overlay=new Bitmap(child.Width,child.Height);child.DrawToBitmap(overlay,child.ClientRectangle);
            var point=root.PointToClient(child.PointToScreen(Point.Empty));graphics.DrawImageUnscaled(overlay,point);
        }
    }
}
