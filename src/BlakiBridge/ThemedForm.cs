using System.Runtime.InteropServices;
namespace BlakiBridge;

public class ThemedForm : Form
{
    public ThemedForm() { AutoScaleDimensions = new SizeF(96,96); AutoScaleMode = AutoScaleMode.Dpi; }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); ApplyFrameTheme(); }
    private void ApplyFrameTheme()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        int allowDark = SystemInformation.HighContrast ? 0 : 1;
        _ = DwmSetWindowAttribute(Handle, 20, ref allowDark, sizeof(int));
    }
    internal int DarkFrameAttribute()
    { return DwmGetWindowAttribute(Handle, 20, out int value, sizeof(int)) >= 0 ? value : -1; }
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0x1A or 0x31A) ApplyFrameTheme();
    }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);
}
