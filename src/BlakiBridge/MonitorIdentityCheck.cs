using System.Text.Json;

namespace BlakiBridge;
internal static class MonitorIdentityCheck
{
    internal static void Run(string output)
    {
        var checks = new List<string>();
        void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks.Add(name); }
        MonitorModel Display(string name, string id, int width = 1920) => new() { DeviceName=name, DeviceIdentity=id,
            Bounds=new(0,0,width,480), WorkingArea=new(0,0,width,480), Primary=false, WidthMm=300, HeightMm=75, DpiX=96, DpiY=96 };
        BridgeConfig Config() => new() { BridgeEnabled=true,
            MonitorIdentities=new() { ["old"]="lian", ["main"]="primary" },
            MonitorPositions=new() { ["old"]=new(1515,1440) }, DiagramWidths=new() { ["old"]=354 },
            DiagramPositions=new() { ["old"]=new(404,356) }, MouseSpeedPercent=new() { ["old"]=120 },
            Connections=[new() { A="old", B="main", Side=Edge.Top }],
            Transitions=[new() { A="old", B="main", Side=Edge.Top, StartA=0,EndA=1919,StartB=2080,EndB=3022,SizeA=new(1920,480),SizeB=new(5120,480) }] };
        using var canvas = new MonitorCanvas();
        var current = new[] { Display("new","lian"), Display("main","primary",5120) };
        var config = Config();
        MonitorIdentity.Calibration(config,current).ToArray();
        MonitorIdentity.Restore(config,canvas,current);
        Check(config.Connections![0].A=="new" && config.Transitions[0].A=="new", "Restart rename restores pair and transition");
        Check(config.SavedTransitions[0].A=="new", "Archived calibration follows rename");
        Check(config.Transitions[0].StartB==2080 && config.Transitions[0].EndB==3022, "Exact calibration endpoints retained");
        Check(config.MonitorPositions["new"].X==1515 && config.DiagramWidths["new"]==354 && config.MouseSpeedPercent["new"]==120, "Per-monitor settings follow identity");
        Check(config.BridgeEnabled && !config.TopologyReviewRequired, "Unambiguous rename does not disable correction");
        config=JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(config))!;
        MonitorIdentity.Restore(config,canvas,current);
        Check(config.MonitorIdentities["new"]=="lian" && config.Transitions.Count==1, "Round trip is idempotent");
        MonitorIdentity.Calibration(config,current).ToArray();config.Transitions.Clear();
        MonitorIdentity.Restore(config,canvas,[current[1]]);
        Check(!MonitorIdentity.Calibration(config,[current[1]]).Any() && config.SavedTransitions.Count==1, "Disconnected calibration retained but inactive");
        config=JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(config))!;
        MonitorIdentity.Restore(config,canvas,current);
        Check(MonitorIdentity.Calibration(config,current).Single().StartB==2080, "Reconnect after restart recovers calibration");
        config=Config();MonitorIdentity.Restore(config,canvas,[Display("new","lian"),Display("other","lian"),current[1]]);
        Check(config.Connections![0].A=="old", "Duplicate hardware identities never guessed");
        config=Config();MonitorIdentity.Calibration(config,[]).ToArray();
        MonitorIdentity.Restore(config,canvas,[Display("old","replacement"),current[1]]);
        Check(!config.BridgeEnabled && config.TopologyReviewRequired && config.Transitions.Count==0, "Reused number with different identity disables correction");
        Check(!MonitorIdentity.Calibration(config,[Display("old","replacement"),current[1]]).Any(), "Replacement cannot inherit archived calibration");
        config=Config();MonitorIdentity.Restore(config,canvas,[Display("main","lian"),Display("old","primary",5120)]);
        Check(config.Connections![0].A=="main" && config.Connections[0].B=="old", "Simultaneous name swap is atomic");
        config=Config();config.MonitorIdentities.Clear();MonitorIdentity.Restore(config,canvas,current);
        Check(config.Connections![0].A=="old", "Legacy unknown identity never guessed from resolution");
        config=Config();config.BridgeEnabled=false;MonitorIdentity.Restore(config,canvas,current);
        Check(!config.BridgeEnabled, "User disabled preference retained");
        File.WriteAllText(output,JsonSerializer.Serialize(new { passed=true, count=checks.Count, checks },new JsonSerializerOptions { WriteIndented=true }));
    }
}
