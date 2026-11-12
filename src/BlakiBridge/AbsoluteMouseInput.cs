using System.Runtime.InteropServices;

namespace BlakiBridge;

internal static class AbsoluteMouseInput
{
    internal static readonly UIntPtr Tag = new(0x424C414B);
    internal static int Normalize(int pixel, int origin, int length)
    {
        if (length <= 0 || length > 65536) throw new ArgumentOutOfRangeException(nameof(length));
        long local = Math.Clamp((long)pixel - origin, 0, length - 1);
        return (int)Math.Min(65535, (local * 65536 + 32768) / length);
    }
    internal static int InputSize => Marshal.SizeOf<Input>();
    internal static bool Move(Point target, Rectangle desktop)
    {
        var input = new Input { Mouse = new MouseInput {
            X = Normalize(target.X, desktop.Left, desktop.Width),
            Y = Normalize(target.Y, desktop.Top, desktop.Height),
            Flags = 0x8000 | 0x4000 | 0x2000 | 1, Extra = Tag
        }};
        return SendInput(1, [input], InputSize) == 1;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
}
