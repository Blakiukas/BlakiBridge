using System.Diagnostics;

namespace BlakiBridge;

internal sealed class ResourceMeter : IDisposable
{
    private readonly Process process = Process.GetCurrentProcess();
    private double lastTime, lastCpu;
    private bool hasSample;
    internal static double CpuPercent(double cpuSeconds, double elapsedSeconds, int processors)
        => elapsedSeconds > 0 && processors > 0 ? Math.Clamp(cpuSeconds / elapsedSeconds / processors * 100, 0, 100) : 0;
    internal (double? Cpu, double Ram) Sample()
    {
        process.Refresh();
        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        double cpu = process.TotalProcessorTime.TotalSeconds;
        double? percent = hasSample ? CpuPercent(cpu - lastCpu, now - lastTime, Environment.ProcessorCount) : null;
        lastTime = now; lastCpu = cpu; hasSample = true;
        return (percent, process.WorkingSet64 / 1000000d);
    }
    public void Dispose() => process.Dispose();
}
