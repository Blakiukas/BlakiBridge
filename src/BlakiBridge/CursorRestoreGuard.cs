using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BlakiBridge;

internal static class CursorRestoreGuard
{
    private static EventWaitHandle? dirty;
    private static EventWaitHandle? heartbeat;
    private static Process? guardian;
    private static string Name(int pid) => "Local\\BlakiBridge.CursorRestore." + pid;
    internal static bool Ensure()
    {
        if (guardian is not null && !guardian.HasExited) return true;
        dirty?.Dispose();
        dirty = new EventWaitHandle(false, EventResetMode.ManualReset, Name(Environment.ProcessId));
        heartbeat?.Dispose();
        heartbeat = new EventWaitHandle(false, EventResetMode.AutoReset, Name(Environment.ProcessId) + ".heartbeat");
        using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, Name(Environment.ProcessId) + ".ready");
        guardian = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--cursor-restore-watch " + Environment.ProcessId) {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
        bool available = guardian is not null && ready.WaitOne(3000) && !guardian.HasExited;
        BridgeTrace.Write("cursor_restore_guard", new { available, pid = guardian?.Id });
        return available;
    }
    internal static void Arm() => dirty!.Set();
    internal static void Disarm() => dirty?.Reset();
    internal static void Renew() => heartbeat?.Set();
    internal static bool Reload() => SystemParametersInfo(0x0057, 0, IntPtr.Zero, 0);
    internal static void Watch(int pid)
    {
        using var parent = Process.GetProcessById(pid);
        using var pending = EventWaitHandle.OpenExisting(Name(pid));
        using var pulse = EventWaitHandle.OpenExisting(Name(pid) + ".heartbeat");
        using var ready = EventWaitHandle.OpenExisting(Name(pid) + ".ready");
        using var exited = new EventWaitHandle(false, EventResetMode.AutoReset);
        exited.SafeWaitHandle = new SafeWaitHandle(parent.Handle, false);
        ready.Set();
        while (true)
        {
            int signal = WaitHandle.WaitAny([exited, pending]);
            if (signal == 0)
            {
                while (pending.WaitOne(0)) { if (Reload()) pending.Reset(); else Thread.Sleep(1000); }
                return;
            }
            // Independent recovery if the owner crashes or hangs while the shared image is blank.
            int recovery;
            do { recovery = WaitHandle.WaitAny([exited, pulse], 500); }
            while (recovery == 1 && pending.WaitOne(0));
            bool ended = recovery == 0;
            if (pending.WaitOne(0) && Reload()) pending.Reset();
            if (ended && !pending.WaitOne(0)) return;
        }
    }
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);
}
