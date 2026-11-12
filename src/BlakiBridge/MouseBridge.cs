using System.Runtime.InteropServices;

namespace BlakiBridge;

internal sealed class MouseBridge : IDisposable
{
    internal sealed record Route(Rectangle Source, Rectangle Target, Edge Edge, Transition Map, bool Reverse);
    private readonly List<Route> routes = [];
    private readonly Hook callback;
    private IntPtr handle;
    private Point previous;
    private bool warping;
    private Point? expected;
    private readonly IReadOnlyList<MonitorModel> displays;
    private readonly BridgeConfig settings;
    private readonly Dictionary<string, SizeF> speedSizes;
    private readonly bool windowsPointerPrecision;
    private double remainderX, remainderY, gapProgress;
    private Route? pendingGap;
    private CursorRedraw.GapVisibility? gapVisibility;
    private void RestoreGapCursor() { gapVisibility?.Dispose(); gapVisibility = null; }
    private readonly CrossingTraceWindow traceWindow = new();
    private long traceSequence;
    public MouseBridge(IEnumerable<Transition> maps, IReadOnlyList<MonitorModel> monitors, BridgeConfig? config = null)
    {
        displays = monitors; settings = config ?? new BridgeConfig();
        speedSizes = monitors.ToDictionary(m => m.DeviceName, m => DisplaySizing.Resolve(m, settings.DiagramWidths, settings.Measurements));
        var mouseSettings = new int[3];
        windowsPointerPrecision = SystemParametersInfo(3, 0, mouseSettings, 0) && mouseSettings[2] != 0;
        callback = OnMouse;
        foreach (var map in maps)
        {
            var a = monitors.FirstOrDefault(m => m.DeviceName == map.A);
            var b = monitors.FirstOrDefault(m => m.DeviceName == map.B);
            if (!map.Valid || a is null || b is null || a.Bounds.Size != map.SizeA || b.Bounds.Size != map.SizeB)
                throw new InvalidOperationException("Display setup changed. Align your screens again.");
            routes.Add(new(a.Bounds, b.Bounds, map.Side, map, false));
            routes.Add(new(b.Bounds, a.Bounds, Transition.Opposite(map.Side), map, true));
        }
        if (routes.Count == 0) throw new InvalidOperationException("Click a screen and finish its alignment first.");
    }
    internal static double SpeedFactor(int pixels, decimal millimeters, int referencePixels, decimal referenceMillimeters)
        => (pixels / (double)millimeters) / (referencePixels / (double)referenceMillimeters);
    internal static double DesktopSpeedFactor(double densityRatio, uint dpi, uint referenceDpi, bool pointerPrecision)
        => densityRatio * (pointerPrecision ? Math.Max(1u, referenceDpi) / (double)Math.Max(1u, dpi) : 1);
    internal static Point ScaleMove(Point from, Point to, double x, double y, ref double rx, ref double ry)
    {
        double dx = (to.X - from.X) * x + rx, dy = (to.Y - from.Y) * y + ry;
        int ix = (int)Math.Round(dx), iy = (int)Math.Round(dy); rx = dx - ix; ry = dy - iy;
        return new Point(from.X + ix, from.Y + iy);
    }
    internal Point Compensate(Route route, Point target, Point proposed)
    {
        var gap = settings.Gaps.GetValueOrDefault(GapSettings.Key(route.Map.A, route.Map.B));
        if (!settings.UsePhysicalGap || gap is not { HasDistance: true } || !route.Target.Contains(target)) { pendingGap = null; gapProgress = 0; return target; }
        string sourceId = route.Reverse ? route.Map.B : route.Map.A, targetId = route.Reverse ? route.Map.A : route.Map.B;
        var source = speedSizes[sourceId];
        double distance = (double)gap.EdgeDistanceMm!.Value;
        bool vertical = Transition.Vertical(route.Edge);
        double pixelsPerMm = vertical ? route.Source.Width / (double)source.Width : route.Source.Height / (double)source.Height;
        double beyond = route.Edge switch { Edge.Left => route.Source.Left - proposed.X, Edge.Right => proposed.X - (route.Source.Right - 1), Edge.Top => route.Source.Top - proposed.Y, _ => proposed.Y - (route.Source.Bottom - 1) };
        if (pendingGap != route) { pendingGap = route; gapProgress = 0; }
        // Keep one pixel inside the edge, so Windows still reports outward movement.
        gapProgress += (Math.Max(0, beyond) + 1) / pixelsPerMm;
        if (gapProgress >= distance) { pendingGap = null; gapProgress = 0; return target; }
        return new Point(Math.Clamp(proposed.X, route.Source.Left + 1, route.Source.Right - 2), Math.Clamp(proposed.Y, route.Source.Top + 1, route.Source.Bottom - 2));
    }
    internal bool IsRunning=>handle!=IntPtr.Zero;
    internal event Action<MouseBridge,Exception>? Faulted;
    internal void Fail(Exception error)
    {
        BridgeTrace.Write("mouse_hook_exception",new{error=error.ToString()});BridgeTrace.ObserveFault("mouse_hook_exception");
        warping=false;Dispose();Faulted?.Invoke(this,error);
    }
    public void Start()
    {
        if (!CursorRestoreGuard.Ensure()) throw new InvalidOperationException("Cursor recovery protection could not start.");
        GetCursorPos(out previous);
        handle = SetWindowsHookEx(14, callback, GetModuleHandle(null), 0);
        if (handle == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        BridgeTrace.Write("crossing_trace_ready", new { mode = "automatic_fault_only", crossingMethod = "erase_move_restore_cursor", safetySamples = 1024, safetyMs = 15000, routes, stopwatchFrequency = System.Diagnostics.Stopwatch.Frequency });
        var referenceMonitor = displays.FirstOrDefault(m => m.Primary) ?? displays[0];
        var referenceSize = speedSizes[referenceMonitor.DeviceName];
        BridgeTrace.Write("speed_reference", new { referenceMonitor.DeviceName, windowsPointerPrecision, factors = displays.Select(m => new { m.DeviceName, m.DpiX, factor = DesktopSpeedFactor(SpeedFactor(m.Bounds.Width, (decimal)speedSizes[m.DeviceName].Width, referenceMonitor.Bounds.Width, (decimal)referenceSize.Width), m.DpiX, referenceMonitor.DpiX, windowsPointerPrecision) }) });
    }
    // Do not turn input missed over an elevated window into a fictitious edge crossing.
    internal static bool InputIsContinuous(Point cached, Point observed, IEnumerable<Rectangle> screens)
        => screens.Any(screen => screen.Contains(cached) && screen.Contains(observed));
    internal static Point? MapMove(Route route, Point from, Point to)
    {
        var s = route.Source;
        if (!s.Contains(from)) return null;
        bool crossing = route.Edge switch
        {
            Edge.Left => to.X <= s.Left && to.X < from.X,
            Edge.Right => to.X >= s.Right - 1 && to.X > from.X,
            Edge.Top => to.Y <= s.Top && to.Y < from.Y,
            _ => to.Y >= s.Bottom - 1 && to.Y > from.Y
        };
        if (!crossing) return null;
        bool vertical = Transition.Vertical(route.Edge);
        double boundary = route.Edge switch { Edge.Left => s.Left, Edge.Right => s.Right - 1, Edge.Top => s.Top, _ => s.Bottom - 1 };
        double fraction = vertical ? (boundary - from.X) / (to.X - from.X) : (boundary - from.Y) / (to.Y - from.Y);
        double along = vertical ? from.Y + fraction * (to.Y - from.Y) - s.Top : from.X + fraction * (to.X - from.X) - s.Left;
        int start = route.Reverse ? route.Map.StartB : route.Map.StartA;
        int end = route.Reverse ? route.Map.EndB : route.Map.EndA;
        if (along < start || along > end)
            return new Point(Math.Clamp(to.X, s.Left, s.Right - 1), Math.Clamp(to.Y, s.Top, s.Bottom - 1));
        int mapped = (int)Math.Round(route.Map.Map(along, route.Reverse));
        var t = route.Target;
        return route.Edge switch
        {
            Edge.Left => new Point(t.Right - 2, t.Top + mapped),
            Edge.Right => new Point(t.Left + 1, t.Top + mapped),
            Edge.Top => new Point(t.Left + mapped, t.Bottom - 2),
            _ => new Point(t.Left + mapped, t.Top + 1)
        };
    }
    private IntPtr OnMouse(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && message == (IntPtr)0x200 && warping && BridgeTrace.Detailed && traceWindow.Active && BridgeTrace.TakeDetailedSample())
        {
            try {
                var nested = Marshal.PtrToStructure<MouseData>(data);
                traceWindow.Add(new { sequence = ++traceSequence, timestamp = System.Diagnostics.Stopwatch.GetTimestamp(), decision = "pass_during_warp", raw = nested.Position, flags = nested.Flags, eventTime = nested.Time });
            } catch { }
        }
        if (code >= 0 && message == (IntPtr)0x200 && !warping)
        {
            try
            {
                var item = Marshal.PtrToStructure<MouseData>(data);
                var position = item.Position;
                if(!BridgeTrace.Detailed && traceWindow.Active)traceWindow.Finish("automatic_diagnostics_complete",previous);
                bool tracing = BridgeTrace.Detailed && traceWindow.Begin(routes, previous, position, Environment.TickCount64) && BridgeTrace.TakeDetailedSample();
                long stamp = tracing ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
                var before = previous; var expectedBefore = expected;
                Route? selectedRoute = null; Point? mappedPoint = null;
                void Trace(string decision, Point? corrected = null, bool? moved = null)
                {
                    if (!tracing) return;
                    try
                    {
                    Point? observed = GetCursorPos(out var cursor) ? cursor : null;
                    traceWindow.Add(new {
                        sequence = ++traceSequence, timestamp = stamp, eventTime = item.Time, flags = item.Flags,
                        before, raw = item.Position, scaled = position, expectedBefore,
                        mappedPoint, corrected, observed, decision, moved,
                        route = selectedRoute is null ? null : new { selectedRoute.Map.A, selectedRoute.Map.B, selectedRoute.Edge, selectedRoute.Reverse },
                        gapProgress, settings.UsePhysicalGap, settings.OptimizeMouseSpeed,
                        elapsedMicroseconds = System.Diagnostics.Stopwatch.GetElapsedTime(stamp).TotalMicroseconds
                    });
                    var effective = decision == "suppress_absolute_input" ? before : moved == true && corrected.HasValue ? corrected.Value : item.Position;
                    var outcome = traceWindow.Outcome(effective, Environment.TickCount64);
                    if (outcome is not null) traceWindow.Finish(outcome, effective);
                    }
                    catch { /* Diagnostics must never disable or change mouse processing. */ }
                }
                if ((item.Flags & 1) != 0 && item.Extra == AbsoluteMouseInput.Tag)
                { Trace("pass_own_absolute_input"); expected = null; previous = position; return CallNextHookEx(handle, code, message, data); }
                if (expected == position) { Trace("pass_expected"); expected = null; previous = position; return CallNextHookEx(handle, code, message, data); }
                if ((item.Flags & 1) != 0) { RestoreGapCursor(); pendingGap = null; gapProgress = 0; Trace("pass_injected"); previous = position; return CallNextHookEx(handle, code, message, data); }
                var raw = position;
                if (!GetCursorPos(out var observed) ||
                    !InputIsContinuous(previous, observed, displays.Select(display => display.Bounds)))
                {
                    RestoreGapCursor(); pendingGap = null; gapProgress = 0;
                    remainderX = remainderY = 0; expected = null;
                    BridgeTrace.Write("input_source_rebased", new { cached = previous, observed, raw });
                    Trace("pass_discontinuous_source");
                    previous = raw;
                    return CallNextHookEx(handle, code, message, data);
                }
                var current = displays.FirstOrDefault(m => m.Bounds.Contains(previous));
                if (settings.OptimizeMouseSpeed && current is not null)
                {
                    var primary = displays.FirstOrDefault(m => m.Primary) ?? displays[0];
                    var size = speedSizes[current.DeviceName]; var reference = speedSizes[primary.DeviceName];
                    // Precision mode already scales Windows movement by monitor DPI.
                    // Remove only that extra scaling before matching physical density.
                    double manual = settings.MouseSpeedFactor(current.DeviceName);
                    if (current.DeviceName == primary.DeviceName && manual == 1) { remainderX = 0; remainderY = 0; }
                    else position = ScaleMove(previous, position,
                        manual * DesktopSpeedFactor(SpeedFactor(current.Bounds.Width, (decimal)size.Width, primary.Bounds.Width, (decimal)reference.Width), current.DpiX, primary.DpiX, windowsPointerPrecision),
                        manual * DesktopSpeedFactor(SpeedFactor(current.Bounds.Height, (decimal)size.Height, primary.Bounds.Height, (decimal)reference.Height), current.DpiY, primary.DpiY, windowsPointerPrecision), ref remainderX, ref remainderY);
                }
                Point? destinationPoint = null;
                foreach (var route in routes)
                {
                    var target = MapMove(route, previous, position);
                    if (target is not Point point) continue;
                    selectedRoute = route; mappedPoint = point;
                    destinationPoint = Compensate(route, point, position);
                    if (route.Target.Contains(destinationPoint.Value))
                    {
                        remainderX = 0; remainderY = 0;
                    }
                    break;
                }
                if (destinationPoint is null && pendingGap is not null)
                {
                    bool retreat = pendingGap.Edge switch { Edge.Left => position.X > previous.X, Edge.Right => position.X < previous.X, Edge.Top => position.Y > previous.Y, _ => position.Y < previous.Y };
                    if (retreat || !pendingGap.Source.Contains(previous)) { pendingGap = null; gapProgress = 0; }
                }
                if (destinationPoint is null && raw != position && current is not null)
                    destinationPoint = new Point(Math.Clamp(position.X, current.Bounds.Left, current.Bounds.Right - 1), Math.Clamp(position.Y, current.Bounds.Top, current.Bounds.Bottom - 1));
                if (destinationPoint is Point final)
                {
                    warping = true;
                    if (pendingGap is not null) gapVisibility ??= CursorRedraw.HideForGap();
                    bool moved = gapVisibility is null && selectedRoute is not null && selectedRoute.Target.Contains(final)
                        ? CursorRedraw.Move(final) : SetCursorPos(final.X, final.Y);
                    if (pendingGap is null || !moved) RestoreGapCursor();
                    if (!moved) { pendingGap = null; gapProgress = 0; BridgeTrace.ObserveFault("cursor_move_failed"); }
                    warping = false;
                    Trace(moved ? "suppress_and_warp" : "warp_failed_pass", final, moved);
                    if (moved) { previous = final; expected = final; return (IntPtr)1; }
                }
                else { if (pendingGap is null) RestoreGapCursor(); Trace("pass_unmapped"); }
                previous = raw;
            }
            catch (Exception ex) { Fail(ex); }
        }
        return CallNextHookEx(handle, code, message, data);
    }
    public void Dispose() { RestoreGapCursor(); pendingGap = null; gapProgress = 0; traceWindow.Finish("engine_stopped_incomplete", previous); if (handle != IntPtr.Zero) { UnhookWindowsHookEx(handle); handle = IntPtr.Zero; } }
    private delegate IntPtr Hook(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] private struct MouseData { public Point Position; public uint Mouse, Flags, Time; public UIntPtr Extra; }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, Hook callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, [Out] int[] values, uint flags);
}

