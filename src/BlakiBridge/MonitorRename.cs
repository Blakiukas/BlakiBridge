namespace BlakiBridge;
internal static class MonitorRename
{
    internal static void Keys<T>(Dictionary<string,T> values,IReadOnlyDictionary<string,string> map)
    {
        var moved=values.Where(k=>map.ContainsKey(k.Key)).Select(k=>new KeyValuePair<string,T>(map[k.Key],k.Value)).ToArray();
        foreach(var key in map.Keys)values.Remove(key);
        foreach(var item in moved)values[item.Key]=item.Value;
    }
    internal static void Apply(BridgeConfig c,MonitorCanvas canvas,IReadOnlyDictionary<string,string> map)
    {
        string Name(string n)=>map.TryGetValue(n,out var value)?value:n;
        Keys(c.MonitorPositions,map);Keys(c.MouseSpeedPercent,map);Keys(c.MouseSpeedPreview,map);Keys(c.Measurements,map);Keys(c.DiagramWidths,map);Keys(c.DiagramPositions,map);
        Keys(c.MonitorIdentities,map);
        Keys(canvas.Positions,map);Keys(canvas.ManualWidths,map);Keys(canvas.Measurements,map);Keys(canvas.DiagramPositions,map);
        foreach(var t in c.Transitions){t.A=Name(t.A);t.B=Name(t.B);}
        foreach(var t in c.SavedTransitions){t.A=Name(t.A);t.B=Name(t.B);}
        if(c.Connections!=null)foreach(var link in c.Connections){link.A=Name(link.A);link.B=Name(link.B);}
        var gaps=c.Gaps.ToArray();c.Gaps.Clear();
        foreach(var pair in gaps.OrderBy(k=>k.Key.Split('|').Any(map.ContainsKey))){var parts=pair.Key.Split('|');c.Gaps[parts.Length==2?GapSettings.Key(Name(parts[0]),Name(parts[1])):pair.Key]=pair.Value;}
    }
}
