namespace BlakiBridge;

internal static class GapSetup
{
    internal static List<Transition> Missing(IEnumerable<Transition> maps, IReadOnlyDictionary<string, GapSettings> values)
        => maps.Where(t => t.Valid).DistinctBy(t => GapSettings.Key(t.A, t.B))
            .Where(t => values.GetValueOrDefault(GapSettings.Key(t.A, t.B)) is not { HasDistance: true }).ToList();
}
