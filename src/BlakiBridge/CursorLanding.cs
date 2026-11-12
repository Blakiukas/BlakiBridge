using System.Runtime.InteropServices;

namespace BlakiBridge;

internal static class CursorLanding
{
    // Only shift perpendicular to the crossed edge. Preserve calibrated along-edge position.
    internal static Point Inset(Rectangle target, Edge travel, Point mapped, Size size, Point hotspot)
    {
        int left = Math.Clamp(hotspot.X, 0, Math.Max(0, size.Width - 1));
        int top = Math.Clamp(hotspot.Y, 0, Math.Max(0, size.Height - 1));
        return travel switch {
            Edge.Left => new Point(Math.Clamp(Math.Min(mapped.X, target.Right - (size.Width - left) - 1), target.Left, target.Right - 1), mapped.Y),
            Edge.Right => new Point(Math.Clamp(Math.Max(mapped.X, target.Left + left + 1), target.Left, target.Right - 1), mapped.Y),
            Edge.Top => new Point(mapped.X, Math.Clamp(Math.Min(mapped.Y, target.Bottom - (size.Height - top) - 1), target.Top, target.Bottom - 1)),
            _ => new Point(mapped.X, Math.Clamp(Math.Max(mapped.Y, target.Top + top + 1), target.Top, target.Bottom - 1))
        };
    }

    internal static bool TryMeasure(out Size size, out Point hotspot)
    {
        size = Size.Empty; hotspot = Point.Empty;
        var cursor = new CursorInfo { Size = Marshal.SizeOf<CursorInfo>() };
        if (!GetCursorInfo(ref cursor) || cursor.Handle == IntPtr.Zero) return false;
        return TryMeasureHandle(cursor.Handle, out size, out hotspot);
    }
    internal static bool TryMeasureHandle(IntPtr handle, out Size size, out Point hotspot)
    {
        size = Size.Empty; hotspot = Point.Empty;
        if (!GetIconInfo(handle, out var icon)) return false;
        try
        {
            var bitmap = icon.Color != IntPtr.Zero ? icon.Color : icon.Mask;
            if (GetObject(bitmap, Marshal.SizeOf<BitmapInfo>(), out var info) == 0) return false;
            int height = Math.Abs(info.Height) / (icon.Color == IntPtr.Zero ? 2 : 1);
            if (info.Width < 1 || height < 1 || info.Width > 1024 || height > 1024) return false;
            size = new Size(info.Width, height); hotspot = new Point((int)icon.X, (int)icon.Y);
            return true;
        }
        finally { if (icon.Color != IntPtr.Zero) DeleteObject(icon.Color); if (icon.Mask != IntPtr.Zero) DeleteObject(icon.Mask); }
    }
    [StructLayout(LayoutKind.Sequential)] private struct CursorInfo { public int Size; public uint Flags; public IntPtr Handle; public Point Position; }
    [StructLayout(LayoutKind.Sequential)] private struct IconInfo { public int IsIcon; public uint X, Y; public IntPtr Mask, Color; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { public int Type, Width, Height, WidthBytes; public ushort Planes, BitsPixel; public IntPtr Bits; }
    [DllImport("user32.dll")] private static extern bool GetCursorInfo(ref CursorInfo info);
    [DllImport("user32.dll")] private static extern bool GetIconInfo(IntPtr cursor, out IconInfo info);
    [DllImport("gdi32.dll", EntryPoint = "GetObjectW")] private static extern int GetObject(IntPtr bitmap, int size, out BitmapInfo info);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
}
