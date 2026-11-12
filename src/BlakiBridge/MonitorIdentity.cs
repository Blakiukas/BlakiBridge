namespace BlakiBridge;

internal static class MonitorIdentity
{
    internal static void Restore(BridgeConfig config, MonitorCanvas canvas, IReadOnlyList<MonitorModel> monitors)
    {
        config.MonitorIdentities ??= new();
        config.SavedTransitions ??= new();
        config.SavedTransitions = config.SavedTransitions.Where(t => t is not null && t.Valid).ToList();
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var old in config.MonitorIdentities)
        {
            if (string.IsNullOrWhiteSpace(old.Value)) continue;
            var matches = monitors.Where(m => string.Equals(m.DeviceIdentity, old.Value, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1 || config.MonitorIdentities.Count(p => string.Equals(p.Value, old.Value, StringComparison.OrdinalIgnoreCase)) != 1) continue;
            if (!string.Equals(old.Key, matches[0].DeviceName, StringComparison.OrdinalIgnoreCase)) names.Add(old.Key, matches[0].DeviceName);
        }
        // A destination still owned by an absent/different monitor is not a safe rename.
        bool collision;
        do
        {
            collision = false;
            foreach (var pair in names.ToArray())
                if (config.MonitorIdentities.ContainsKey(pair.Value) && !names.ContainsKey(pair.Value))
                { names.Remove(pair.Key); collision = true; }
        } while (collision);
        if (names.Count > 0) MonitorRename.Apply(config, canvas, names);

        foreach (var monitor in monitors)
        {
            if (config.MonitorIdentities.TryGetValue(monitor.DeviceName, out var identity)
                && !string.Equals(identity, monitor.DeviceIdentity, StringComparison.OrdinalIgnoreCase))
            {
                // Never transfer calibration to a different device reusing a DISPLAY number.
                config.TopologyReviewRequired = true;
                config.BridgeEnabled = false;
                config.Transitions.RemoveAll(t => t.A == monitor.DeviceName || t.B == monitor.DeviceName);
                continue;
            }
            if (!string.IsNullOrWhiteSpace(monitor.DeviceIdentity)) config.MonitorIdentities[monitor.DeviceName] = monitor.DeviceIdentity;
        }
    }

    internal static IEnumerable<Transition> Calibration(BridgeConfig config, IReadOnlyList<MonitorModel> monitors)
    {
        config.SavedTransitions ??= new();
        foreach (var map in config.Transitions.Where(t => t.Valid))
        {
            config.SavedTransitions.RemoveAll(t => GapSettings.Key(t.A, t.B) == GapSettings.Key(map.A, map.B));
            config.SavedTransitions.Add(Clone(map));
        }
        return config.SavedTransitions.Where(t => t.Valid && new[] { t.A, t.B }.All(name =>
            monitors.Any(m => m.DeviceName == name && (!config.MonitorIdentities.TryGetValue(name, out var identity)
                || string.Equals(identity, m.DeviceIdentity, StringComparison.OrdinalIgnoreCase)))));
    }

    private static Transition Clone(Transition t) => new() { A=t.A, B=t.B, Side=t.Side, StartA=t.StartA, EndA=t.EndA,
        StartB=t.StartB, EndB=t.EndB, SizeA=t.SizeA, SizeB=t.SizeB };
}
