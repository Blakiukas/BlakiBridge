namespace BlakiBridge;

// Keeps the calibrated session baseline immutable while Windows rebuilds display topology.
internal sealed class DisplayRecovery
{
    private sealed record Display(string Name, string Identity, Rectangle Bounds, bool Primary, uint DpiX, uint DpiY);
    private Display[] baseline = [];
    private string? previous;
    private bool reviewing;
    internal bool AcceptedDifferentSetup {get;private set;}
    internal void BeginReview(){Begin(false);reviewing=true;}
    public bool Pending { get; private set; }
    public bool Suspended { get; private set; }
    public int Attempts { get; private set; }
    public bool ShouldPoll => Pending && !Suspended;
    internal int PollInterval => Attempts < 30 ? 1000 : 15000;
    internal string Reason {get;private set;}="waiting";
    internal Dictionary<string,string> Renames {get;private set;}=new(StringComparer.OrdinalIgnoreCase);
    public void SetBaseline(IReadOnlyList<MonitorModel> monitors) => baseline = Snapshot(monitors);
    private static Display[] Snapshot(IReadOnlyList<MonitorModel> monitors) => monitors.Select(m => new Display(m.DeviceName, m.DeviceIdentity, m.Bounds, m.Primary, m.DpiX, m.DpiY)).OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    public void Begin(bool suspended)
    { Pending = true; Suspended = suspended; Attempts = 0; previous = null; reviewing=false;AcceptedDifferentSetup=false;Renames.Clear();Reason=suspended?"suspended":"waiting"; }
    public bool Observe(IReadOnlyList<MonitorModel> monitors)
    {
        if (!ShouldPoll) return false;
        Attempts++;
        var current = Snapshot(monitors);
        var names=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        bool unique=baseline.All(m=>!string.IsNullOrWhiteSpace(m.Identity)) && current.All(m=>!string.IsNullOrWhiteSpace(m.Identity))
            && baseline.Select(m=>m.Identity).Distinct(StringComparer.OrdinalIgnoreCase).Count()==baseline.Length
            && current.Select(m=>m.Identity).Distinct(StringComparer.OrdinalIgnoreCase).Count()==current.Length
            && current.Select(m=>m.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()==current.Length;
        bool compatible=baseline.Length>0 && baseline.Length==current.Length && unique;
        Reason=baseline.Length!=current.Length?"missing":!unique?"identity":"geometry";
        if(compatible)foreach(var old in baseline){var match=current.SingleOrDefault(m=>string.Equals(old.Identity,m.Identity,StringComparison.OrdinalIgnoreCase));
            if(match is null){compatible=false;Reason="identity";break;}
            if(match.Bounds!=old.Bounds||match.Primary!=old.Primary){compatible=false;break;}
            if(!string.Equals(match.Name,old.Name,StringComparison.OrdinalIgnoreCase))names.Add(old.Name,match.Name);
        }
        if(compatible)Reason="stabilizing";
        string signature = System.Text.Json.JsonSerializer.Serialize(current);
        bool acceptable=compatible || reviewing && current.Length>0 && current.All(m=>!string.IsNullOrWhiteSpace(m.Identity)) && current.Select(m=>m.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()==current.Length;
        bool stable = acceptable && signature == previous;
        previous = acceptable ? signature : null;
        if (stable) { Pending = false;AcceptedDifferentSetup=!compatible;Renames=compatible?names:new(StringComparer.OrdinalIgnoreCase); return true; }
        return false;
    }
}