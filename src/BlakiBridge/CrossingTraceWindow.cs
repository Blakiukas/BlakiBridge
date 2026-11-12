namespace BlakiBridge;

internal sealed class CrossingTraceWindow
{
    internal MouseBridge.Route? Route { get; private set; }
    internal bool Active => Route is not null;
    private readonly List<object> samples = [];
    private Point start;
    private long started;
    private bool startedInside;
    internal static int Depth(Rectangle bounds, Edge edge, Point p) => edge switch {
        Edge.Left => p.X - bounds.Left, Edge.Right => bounds.Right - 1 - p.X,
        Edge.Top => p.Y - bounds.Top, _ => bounds.Bottom - 1 - p.Y };
    internal bool Begin(IEnumerable<MouseBridge.Route> routes, Point before, Point raw, long now)
    {
        if (Active) return true;
        var route = routes.FirstOrDefault(r => r.Source.Contains(before) &&
            Depth(r.Source, r.Edge, raw) <= 50 && Depth(r.Source, r.Edge, raw) < Depth(r.Source, r.Edge, before));
        if (route is null) return false;
        Route = route; start = before; started = now;
        startedInside = Depth(route.Source, route.Edge, before) < 50;
        return true;
    }
    internal string? Outcome(Point effective, long now)
    {
        if (Route is not { } r) return null;
        if (r.Target.Contains(effective) && Depth(r.Target, Transition.Opposite(r.Edge), effective) >= 50) return "completed";
        if (r.Source.Contains(effective) && Depth(r.Source, r.Edge, effective) > 50) return "retreated";
        if (!r.Source.Contains(effective) && !r.Target.Contains(effective)) return "left_pair";
        if (samples.Count >= 65536 || now - started > 30000) return "safety_limit_incomplete";
        return null;
    }
    internal void Add(object sample) { if (Active && samples.Count < 65536) samples.Add(sample); }
    internal void Finish(string reason, Point end)
    {
        if (!Active) return;
        BridgeTrace.Write("crossing_session", new { reason, startedInside, start, end, started, ended = Environment.TickCount64, route = Route, samples = samples.ToArray() });
        Route = null; samples.Clear();
    }
}
