using System.Text.Json;

namespace BlakiBridge;

public sealed class BridgeConfig
{
    public bool? WelcomeGuideShown { get; set; }
    public bool MouseControlAdviceShown { get; set; }
    public string Language { get; set; } = "EN";
    public Dictionary<string, Point> MonitorPositions { get; set; } = new();
    public bool BridgeEnabled { get; set; }
    public bool AutoPhysicalSize { get; set; } = true;
    public bool OptimizeMouseSpeed { get; set; }
    public Dictionary<string, decimal> MouseSpeedPercent { get; set; } = new();
    internal readonly Dictionary<string, decimal> MouseSpeedPreview = new();
    internal double MouseSpeedFactor(string device) => (double)Math.Clamp(MouseSpeedPreview.GetValueOrDefault(device, MouseSpeedPercent.GetValueOrDefault(device, 100m)), 25m, 300m) / 100;
    public bool UsePhysicalGap { get; set; }
    public Dictionary<string, DisplayMeasurements> Measurements { get; set; } = new();
    public Dictionary<string, GapSettings> Gaps { get; set; } = new();
    public Dictionary<string, float> DiagramWidths { get; set; } = new();
    public Dictionary<string, PointF> DiagramPositions { get; set; } = new();
    public List<MonitorLink>? Connections { get; set; }
    public bool TopologyReviewRequired { get; set; }
    public List<Transition> Transitions { get; set; } = new();
    public Dictionary<string, string> MonitorIdentities { get; set; } = new();
    public List<Transition> SavedTransitions { get; set; } = new();
    public static string FilePath => Path.Combine(AppStorage.DirectoryPath, "config.json");

    public static BridgeConfig Load(string? path = null)
    {
        path ??= FilePath;
        try
        {
            var result = JsonSerializer.Deserialize<BridgeConfig>(File.ReadAllText(path)) ?? new();
            result.MonitorPositions ??= new();
            result.MouseSpeedPercent ??= new(); result.Measurements ??= new(); result.Gaps ??= new();
            result.DiagramWidths ??= new(); result.DiagramPositions ??= new();
            result.Connections=result.Connections?.Where(l=>l!=null && l.A!=l.B && !string.IsNullOrEmpty(l.A) && !string.IsNullOrEmpty(l.B) && Enum.IsDefined(l.Side)).DistinctBy(l=>GapSettings.Key(l.A,l.B)).ToList();
            result.Transitions = (result.Transitions ?? []).Where(t => t is not null && t.Valid).ToList();
            return result;
        }
        catch (Exception error)
        {
            if (path == FilePath) BridgeTrace.Write("config_load_fallback", new { error = error.Message });
            try
            {
                var result = JsonSerializer.Deserialize<BridgeConfig>(File.ReadAllText(path + ".bak")) ?? new();
                result.MonitorPositions ??= new();
                result.MouseSpeedPercent ??= new(); result.Measurements ??= new(); result.Gaps ??= new();
                result.DiagramWidths ??= new(); result.DiagramPositions ??= new();
                result.Connections=result.Connections?.Where(l=>l!=null && l.A!=l.B && !string.IsNullOrEmpty(l.A) && !string.IsNullOrEmpty(l.B) && Enum.IsDefined(l.Side)).DistinctBy(l=>GapSettings.Key(l.A,l.B)).ToList();
            result.Transitions = (result.Transitions ?? []).Where(t => t is not null && t.Valid).ToList();
                return result;
            }
            catch (Exception backupError)
            {
                if (path == FilePath) BridgeTrace.Write("config_load_defaults", new { error = backupError.Message });
                return new();
            }
        }
    }

    public void Save(string? path = null)
    {
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak", true);
        else File.Move(temporary, path);
    }
}
