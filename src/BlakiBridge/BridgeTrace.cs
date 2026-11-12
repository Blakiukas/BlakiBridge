using System.Collections.Concurrent;
using System.Text.Json;

namespace BlakiBridge;

internal static class BridgeTrace
{
    internal static string ForegroundProcess()
    {
        try { GetWindowThreadProcessId(GetForegroundWindow(), out uint pid); using var process = System.Diagnostics.Process.GetProcessById((int)pid); return process.ProcessName; }
        catch { return "unavailable"; }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    internal static bool Enabled;
    private static readonly AutomaticDiagnostics automatic=new();
    internal static bool Detailed=>Enabled && automatic.Active(Environment.TickCount64);
    internal static bool TakeDetailedSample()=>Enabled && automatic.Take(Environment.TickCount64);
    internal static void ObserveFault(string reason)
    {if(Enabled && automatic.Trigger(Environment.TickCount64))Write("automatic_diagnostics_started",new{reason,seconds=15,maxSamples=1024});}
    private sealed record Entry(DateTime utc, int pid, string name, object? data);
    private static readonly ConcurrentQueue<Entry> queue = new();
    private static int writing;
    internal static void Write(string name, object? data = null)
    {
        if (!Enabled || queue.Count >= 128) return;
        try { queue.Enqueue(new Entry(DateTime.UtcNow, Environment.ProcessId, name, data)); }
        catch { return; }
        StartWriter();
    }
    private static void StartWriter()
    {
        if (Interlocked.CompareExchange(ref writing, 1, 0) != 0) return;
        _ = Task.Run(() =>
        {
            try
            {
                string directory = Path.Combine(AppStorage.DirectoryPath, "diagnostics");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "events.jsonl");
                while (queue.TryDequeue(out var line))
                {
                    if (File.Exists(path) && new FileInfo(path).Length >= 1024 * 1024) File.Move(path, path + ".previous", true);
                    File.AppendAllText(path, JsonSerializer.Serialize(line) + Environment.NewLine);
                }
            }
            catch { while (queue.TryDequeue(out _)) { } }
            finally { Interlocked.Exchange(ref writing, 0); if (!queue.IsEmpty) StartWriter(); }
        });
    }
}
