namespace BlakiBridge;

internal static class DisplaySizing
{
    // The same proportions drive both the diagram and speed correction.
    internal static SizeF Resolve(MonitorModel monitor, IReadOnlyDictionary<string, float> widths, IReadOnlyDictionary<string, DisplayMeasurements> measurements)
    {
        if (widths.TryGetValue(monitor.DeviceName, out float manual) && float.IsFinite(manual) && manual >= 40 && manual <= 4000)
            return new SizeF(manual, manual * monitor.Bounds.Height / monitor.Bounds.Width);
        if (measurements.GetValueOrDefault(monitor.DeviceName) is { Valid: true } physical)
            return new SizeF((float)physical.Width, (float)physical.Height);
        float width = monitor.PhysicalSizeReported ? monitor.WidthMm : monitor.Bounds.Width * 25.4f / 96;
        width = Math.Clamp(width, 40, 4000);
        return new SizeF(width, width * monitor.Bounds.Height / monitor.Bounds.Width);
    }
}
