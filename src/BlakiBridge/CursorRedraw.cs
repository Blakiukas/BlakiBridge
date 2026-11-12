using System.Runtime.InteropServices;

namespace BlakiBridge;

internal static class CursorRedraw
{
    // A gap lasts across multiple hook callbacks. Never snapshot its blank image
    // again with Move: retain the original until the crossing ends or is cancelled.
    internal sealed class GapVisibility : IDisposable
    {
        private IntPtr saved;
        private readonly uint id;
        private readonly List<(uint Id, IntPtr Image)> otherImages = [];
        private readonly System.Windows.Forms.Timer pulse = new() { Interval = 100 };
        internal GapVisibility(IntPtr image, uint systemId)
        {
            saved = image; id = systemId;
            // Windows may change from arrow to resize/hand while crossing a window edge.
            // Keep every standard shape blank for the same lease, restoring exact copies.
            foreach (uint other in SystemIds.Where(value => value != id))
            {
                IntPtr original = CopyIcon(LoadCursor(IntPtr.Zero, (IntPtr)other));
                if (original == IntPtr.Zero) continue;
                IntPtr blank = CreateCursor(IntPtr.Zero, 0, 0, 32, 32, Enumerable.Repeat((byte)255,128).ToArray(), new byte[128]);
                if (blank == IntPtr.Zero) { DestroyCursor(original); continue; }
                if (SetSystemCursor(blank, other)) otherImages.Add((other, original));
                else DestroyCursor(original);
            }
            pulse.Tick += (_, _) => CursorRestoreGuard.Renew();
            CursorRestoreGuard.Renew();
            pulse.Start();
        }
        public void Dispose()
        {
            if (saved == IntPtr.Zero) return;
            pulse.Stop(); pulse.Dispose();
            bool restored = SetSystemCursor(saved, id); saved = IntPtr.Zero;
            foreach (var other in otherImages) restored = SetSystemCursor(other.Image, other.Id) && restored;
            otherImages.Clear();
            if (!restored) restored = CursorRestoreGuard.Reload();
            if (restored) CursorRestoreGuard.Disarm();
            BridgeTrace.Write("gap_cursor_restored", new { restored });
        }
    }
    internal static GapVisibility? HideForGap()
    {
        var current = Read();
        if (current is not { Flags: 1 }) return null;
        uint id = SystemIds.FirstOrDefault(i => LoadCursor(IntPtr.Zero, (IntPtr)i) == current.Value.Handle);
        if (id == 0) return null;
        IntPtr saved = CopyIcon(current.Value.Handle);
        IntPtr blank = CreateCursor(IntPtr.Zero, 0, 0, 32, 32, Enumerable.Repeat((byte)255,128).ToArray(), new byte[128]);
        if (saved == IntPtr.Zero || blank == IntPtr.Zero)
        {
            if (saved != IntPtr.Zero) DestroyCursor(saved);
            if (blank != IntPtr.Zero) DestroyCursor(blank);
            return null;
        }
        CursorRestoreGuard.Arm();
        if (!SetSystemCursor(blank, id))
        {
            DestroyCursor(saved); CursorRestoreGuard.Disarm(); return null;
        }
        try
        {
            var lease = new GapVisibility(saved, id);
            BridgeTrace.Write("gap_cursor_hidden", new { systemId = id });
            return lease;
        }
        catch
        {
            if (SetSystemCursor(saved, id) || CursorRestoreGuard.Reload()) CursorRestoreGuard.Disarm();
            throw;
        }
    }
    internal static bool Probe()
    {
        var current = Read();
        if (current.HasValue)
        {
            AbsoluteMouseInput.Move(current.Value.Position, SystemInformation.VirtualScreen);
            System.Threading.Thread.Sleep(30);
        }
        if (!current.HasValue || !Move(current.Value.Position, true)) return false;
        string? held = null, restored = null;
        var originals = SystemIds.ToDictionary(id => id, id => Snapshot(LoadCursor(IntPtr.Zero, (IntPtr)id), "original-shape-" + id));
        using (var gap = HideForGap())
        {
            if (gap is null) return false;
            var until = Environment.TickCount64 + 900;
            while (Environment.TickCount64 < until) { Application.DoEvents(); Thread.Sleep(10); }
            var state = Read();
            if (state.HasValue) held = Snapshot(state.Value.Handle, "gap-held");
            foreach (uint id in SystemIds)
                if (!Snapshot(LoadCursor(IntPtr.Zero, (IntPtr)id), "gap-shape-" + id).StartsWith("ink=0;"))
                    throw new InvalidOperationException("Standard cursor shape remained visible during gap: " + id);
        }
        var after = Read();
        foreach (uint id in SystemIds)
            if (Snapshot(LoadCursor(IntPtr.Zero, (IntPtr)id), "restored-shape-" + id) != originals[id])
                throw new InvalidOperationException("Cursor shape was not restored exactly: " + id);
        if (after.HasValue) restored = Snapshot(after.Value.Handle, "gap-restored");
        BridgeTrace.Write("gap_cursor_probe", new { held, restored, durationMs = 900 });
        return held is not null && held.StartsWith("ink=0;") && restored is not null && !restored.StartsWith("ink=0;");
    }
    internal static bool Execute(Action hide, Func<bool> move, Action restore)
    {
        try { hide(); return move(); }
        finally { restore(); }
    }
    internal static bool Move(Point target, bool verifyImage = false)
    {
        var before = Read();
        // Respect applications that intentionally hide their cursor (e.g. games).
        if (before is not { Flags: 1 } || before.Value.Handle == IntPtr.Zero)
        {
            bool fallback = SetCursorPos(target.X,target.Y);
            BridgeTrace.Write("cursor_redraw_skipped", new { reason = "cursor_not_visible_or_unavailable", moved = fallback });
            return fallback;
        }
        uint systemId = SystemIds.FirstOrDefault(id => LoadCursor(IntPtr.Zero, (IntPtr)id) == before.Value.Handle);
        if (systemId == 0)
        {
            BridgeTrace.Write("cursor_redraw_skipped", new { reason = "non_system_cursor" });
            return SetCursorPos(target.X,target.Y);
        }
        var saved = CopyIcon(before.Value.Handle);
        var blank = CreateCursor(IntPtr.Zero, 0, 0, 32, 32, Enumerable.Repeat((byte)255,128).ToArray(), new byte[128]);
        if (saved == IntPtr.Zero || blank == IntPtr.Zero)
        {
            if (saved != IntPtr.Zero) DestroyCursor(saved);
            if (blank != IntPtr.Zero) DestroyCursor(blank);
            return SetCursorPos(target.X,target.Y);
        }
        CursorInfo? hidden = null, after = null;
        bool replaced = false, restored = false;
        var originalImage = verifyImage ? Snapshot(before.Value.Handle, "before") : null;
        string? hiddenImage = null, restoredImage = null;
        CursorRestoreGuard.Arm();
        bool moved = Execute(
            () => { replaced = SetSystemCursor(blank, systemId); blank = IntPtr.Zero; hidden = Read(); if (verifyImage) hiddenImage = Snapshot(LoadCursor(IntPtr.Zero, (IntPtr)systemId), "hidden"); },
            () => SetCursorPos(target.X,target.Y),
            () => {
                restored = SetSystemCursor(saved, systemId); saved = IntPtr.Zero;
                if (!restored) restored = CursorRestoreGuard.Reload();
                if (restored) CursorRestoreGuard.Disarm();
                after = Read();
                if (verifyImage) restoredImage = Snapshot(LoadCursor(IntPtr.Zero, (IntPtr)systemId), "restored");
            });
        BridgeTrace.Write("cursor_redraw", new {
            target, moved, before = Describe(before), hidden = Describe(hidden), after = Describe(after),
            systemId, blankImageApplied = replaced,
            restoredConfirmed = restored, originalImage, hiddenImage, restoredImage
        });
        return moved;
    }
    private static string Snapshot(IntPtr cursor, string phase)
    {
        using var bitmap = new Bitmap(96,96);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Magenta);
            IntPtr dc=graphics.GetHdc();
            try { if (!DrawIconEx(dc,0,0,cursor,0,0,0,IntPtr.Zero,3)) throw new InvalidOperationException("Cursor image could not be rendered."); }
            finally { graphics.ReleaseHdc(dc); }
        }
        uint hash=2166136261; int ink=0;
        for(int y=0;y<96;y++) for(int x=0;x<96;x++) { int pixel=bitmap.GetPixel(x,y).ToArgb(); if(pixel!=Color.Magenta.ToArgb()) ink++; hash=unchecked((hash^(uint)pixel)*16777619); }
        bitmap.Save(Path.Combine(AppContext.BaseDirectory,"cursor-redraw-"+phase+".png"));
        return $"ink={ink};hash={hash:X8}";
    }
    private static readonly uint[] SystemIds = [32512,32513,32514,32515,32516,32642,32643,32644,32645,32646,32648,32649,32650];
    private static object? Describe(CursorInfo? state) => state.HasValue ? new { state.Value.Flags, handle = state.Value.Handle.ToInt64(), state.Value.Position } : null;
    private static CursorInfo? Read()
    {
        var state = new CursorInfo { Size = Marshal.SizeOf<CursorInfo>() };
        return GetCursorInfo(ref state) ? state : null;
    }
    [StructLayout(LayoutKind.Sequential)] private struct CursorInfo { public int Size; public uint Flags; public IntPtr Handle; public Point Position; }
    [DllImport("user32.dll")] private static extern bool GetCursorInfo(ref CursorInfo info);
    [DllImport("user32.dll")] private static extern IntPtr SetCursor(IntPtr cursor);
    [DllImport("user32.dll")] private static extern IntPtr LoadCursor(IntPtr instance, IntPtr id);
    [DllImport("user32.dll")] private static extern IntPtr CopyIcon(IntPtr cursor);
    [DllImport("user32.dll")] private static extern IntPtr CreateCursor(IntPtr instance, int x, int y, int width, int height, byte[] andMask, byte[] xorMask);
    [DllImport("user32.dll")] private static extern bool SetSystemCursor(IntPtr cursor, uint id);
    [DllImport("user32.dll")] private static extern bool DestroyCursor(IntPtr cursor);
    [DllImport("user32.dll")] private static extern bool DrawIconEx(IntPtr dc,int x,int y,IntPtr icon,int width,int height,uint frame,IntPtr brush,uint flags);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x,int y);
}
