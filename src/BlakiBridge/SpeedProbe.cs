using System.Runtime.InteropServices;
using System.Text.Json;
namespace BlakiBridge;

// Explicit opt-in diagnostic only; never used by the engine or silent tests.
internal static class SpeedProbe
{
    internal static void Run(string output)
    {
        if (!GetCursorPos(out var original)) throw new InvalidOperationException("Cannot read cursor");
        var samples = new List<object>();
        try
        {
            foreach (var monitor in MonitorModel.Detect())
            foreach (int delta in new[] { 2, 8, 24 })
            {
                var start = new Point(monitor.Bounds.Left + monitor.Bounds.Width / 2, monitor.Bounds.Top + monitor.Bounds.Height / 2);
                if (!SetCursorPos(start.X, start.Y)) throw new InvalidOperationException("Cannot place probe");
                Thread.Sleep(80);
                var input = new Input { Mouse = new MouseInput { X = delta, Flags = 1 | 0x2000 } };
                if (SendInput(1, [input], Marshal.SizeOf<Input>()) != 1) throw new InvalidOperationException("Input probe blocked");
                Thread.Sleep(40); GetCursorPos(out var end);
                samples.Add(new { monitor.DeviceName, monitor.DpiX, delta, actualX = end.X - start.X, actualY = end.Y - start.Y });
            }
        }
        finally { SetCursorPos(original.X, original.Y); }
        File.WriteAllText(output, JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));
    }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X,Y; public uint Data,Flags,Time; public UIntPtr Extra; }
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
}
