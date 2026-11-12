using System.Runtime.InteropServices;

namespace BlakiBridge;

public sealed class MonitorModel
{
    internal static string WmiStatus = "";
    public required string DeviceName { get; init; }
    public required Rectangle Bounds { get; init; }
    public required Rectangle WorkingArea { get; init; }
    public required bool Primary { get; init; }
    public required int WidthMm { get; init; }
    public required int HeightMm { get; init; }
    public required uint DpiX { get; init; }
    public required uint DpiY { get; init; }
    public bool PhysicalSizeReported { get; init; }
    public string DeviceIdentity { get; init; } = "";
    public string ModelName { get; init; } = "Display";
    public string SizeSources { get; init; } = "No physical-size data.";
    public double PhysicalDpiX => WidthMm > 0 ? Bounds.Width / (WidthMm / 25.4) : 0;
    public double PhysicalDpiY => HeightMm > 0 ? Bounds.Height / (HeightMm / 25.4) : 0;

    // Recovery needs fresh identity/geometry/DPI only; avoid repeated WMI and EDID scans.
    internal static IReadOnlyList<MonitorModel> DetectRecoverySnapshot(IReadOnlyList<MonitorModel> baseline)
    {
        return Screen.AllScreens.Select(screen =>
        {

            var device = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
            string identity = EnumDisplayDevices(screen.DeviceName, 0, ref device, 1) ? device.DeviceID ?? "" : "";
            var matches=baseline.Where(m=>!string.IsNullOrWhiteSpace(identity)&&string.Equals(m.DeviceIdentity,identity,StringComparison.OrdinalIgnoreCase)).ToArray();
            var old=matches.Length==1?matches[0]:null;
            var handle = MonitorFromPoint(new POINT(screen.Bounds.Left + 1, screen.Bounds.Top + 1), 2);
            uint dx = 96, dy = 96;
            if (handle == IntPtr.Zero || GetDpiForMonitor(handle, 0, out dx, out dy) != 0)
                throw new InvalidOperationException("Display DPI not ready");
            return new MonitorModel { DeviceName = screen.DeviceName, DeviceIdentity = identity, Bounds = screen.Bounds, WorkingArea = screen.WorkingArea, Primary = screen.Primary, DpiX = dx, DpiY = dy, WidthMm = old?.WidthMm ?? 0, HeightMm = old?.HeightMm ?? 0, PhysicalSizeReported = old?.PhysicalSizeReported ?? false, ModelName = old?.ModelName ?? screen.DeviceName, SizeSources = old?.SizeSources ?? "" };
        }).ToArray();
    }
    public static IReadOnlyList<MonitorModel> Detect()
    {
        var result = new List<MonitorModel>();
        var wmiSizes = ReadWmiSizes();
        foreach (var screen in Screen.AllScreens)
        {
            uint dx = 96, dy = 96;
            int widthMm = 0, heightMm = 0;
            var point = new POINT(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var handle = MonitorFromPoint(point, 2);
            if (handle != IntPtr.Zero)
                _ = GetDpiForMonitor(handle, 0, out dx, out dy);

            var dc = CreateDC("DISPLAY", screen.DeviceName, null, IntPtr.Zero);
            if (dc != IntPtr.Zero)
            {
                widthMm = GetDeviceCaps(dc, 4);
                heightMm = GetDeviceCaps(dc, 6);
                DeleteDC(dc);
            }

            bool reported = false;
            string model = screen.DeviceName;
            var sources = new List<string> { $"Windows driver: {widthMm} × {heightMm} mm (may be estimated)" };
            if (WmiStatus.Length > 0) sources.Add(WmiStatus);
            var device = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
            if (EnumDisplayDevices(screen.DeviceName, 0, ref device, 1))
            {
                model = device.DeviceString;
                try
                {
                    var parts = device.DeviceID.Split('#');
                    if (parts.Length >= 3)
                    {
                        string instance = "DISPLAY\\" + parts[1] + "\\" + parts[2];
                        var alternative = wmiSizes.FirstOrDefault(p => p.Key.StartsWith(instance + "_", StringComparison.OrdinalIgnoreCase));
                        if (alternative.Key is not null)
                        {
                            int aw = alternative.Value.Width, ah = alternative.Value.Height;
                            sources.Add($"Windows WMI: {aw} × {ah} mm");
                            if ((screen.Bounds.Width > screen.Bounds.Height) != (aw > ah)) (aw, ah) = (ah, aw);
                            if (aw >= 30 && ah >= 30 && aw <= 4000 && ah <= 4000 && Math.Abs(aw / (double)ah / (screen.Bounds.Width / (double)screen.Bounds.Height) - 1) < .05)
                            { widthMm = aw; heightMm = ah; reported = true; }
                        }
                        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY\" + parts[1] + "\\" + parts[2] + @"\Device Parameters");
                        if (key?.GetValue("EDID") is byte[] edid && edid.Length >= 128 && edid.Take(128).Sum(v => v) % 256 == 0)
                        {
                            int w = edid[21] * 10, h = edid[22] * 10;
                            sources.Add($"EDID basic: {w} × {h} mm");
                            for (int o = 54; o <= 108; o += 18)
                                if (edid[o] == 0 && edid[o + 1] == 0 && edid[o + 3] == 0xFC)
                                    model = System.Text.Encoding.ASCII.GetString(edid, o + 5, 13).Trim('\0', '\n', ' ', '\r');
                            for (int offset = 54; offset <= 108; offset += 18)
                            {
                                if (edid[offset] == 0 && edid[offset + 1] == 0) continue;
                                int tw = edid[offset + 12] + ((edid[offset + 14] & 0xF0) << 4);
                                int th = edid[offset + 13] + ((edid[offset + 14] & 0x0F) << 8);
                                sources.Add($"EDID timing: {tw} × {th} mm");
                                double expected = Math.Max(screen.Bounds.Width, screen.Bounds.Height) / (double)Math.Min(screen.Bounds.Width, screen.Bounds.Height);
                                if (tw > 0 && th > 0 && Math.Abs(Math.Max(tw, th) / (double)Math.Min(tw, th) / expected - 1) < .05) { w = tw; h = th; break; }
                            }
                            if (w >= 30 && h >= 30 && w <= 4000 && h <= 4000)
                            {
                                if ((screen.Bounds.Width > screen.Bounds.Height) != (w > h)) (w, h) = (h, w);
                                double ratio = screen.Bounds.Width / (double)screen.Bounds.Height;
                                if (Math.Abs(w / (double)h / ratio - 1) < .05)
                                { widthMm = w; heightMm = h; reported = true; }
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException) { }
            }
            if (string.IsNullOrWhiteSpace(model) || model.Contains("Generic", StringComparison.OrdinalIgnoreCase) || model == screen.DeviceName)
            {
                for (uint adapterIndex=0; ; adapterIndex++)
                {
                    var adapter=new DISPLAY_DEVICE { cb=Marshal.SizeOf<DISPLAY_DEVICE>() };
                    if(!EnumDisplayDevices(null,adapterIndex,ref adapter,0))break;
                    if(adapter.DeviceName==screen.DeviceName && !string.IsNullOrWhiteSpace(adapter.DeviceString))
                    { model=adapter.DeviceString; break; }
                }
            }
            result.Add(new MonitorModel
            {
                DeviceName = screen.DeviceName,
                DeviceIdentity = device.DeviceID ?? "",
                Bounds = screen.Bounds,
                WorkingArea = screen.WorkingArea,
                Primary = screen.Primary,
                WidthMm = widthMm,
                HeightMm = heightMm,
                DpiX = dx,
                DpiY = dy
                ,PhysicalSizeReported = reported
                ,ModelName = model, SizeSources = string.Join("\n", sources.Distinct())
            });
        }
        return result;
    }
    private static Dictionary<string, Size> ReadWmiSizes()
    {
        WmiStatus = "";
        var result = new Dictionary<string, Size>(StringComparer.OrdinalIgnoreCase);
        object? locator = null, services = null, rows = null;
        try
        {
            var type = Type.GetTypeFromProgID("WbemScripting.SWbemLocator");
            if (type is null) return result;
            locator = Activator.CreateInstance(type)!;
            services = ((dynamic)locator).ConnectServer(".", @"root\wmi");
            rows = ((dynamic)services).ExecQuery("SELECT * FROM WmiMonitorBasicDisplayParams", "WQL", 16);
            foreach (object row in (System.Collections.IEnumerable)rows)
            {
                try
                {
                    dynamic item = row;
                    result[(string)item.Properties_.Item("InstanceName").Value] = new Size(Convert.ToInt32(item.Properties_.Item("MaxHorizontalImageSize").Value) * 10, Convert.ToInt32(item.Properties_.Item("MaxVerticalImageSize").Value) * 10);
                }
                finally { Marshal.ReleaseComObject(row); }
            }
        }
        catch (Exception ex) when (ex is COMException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException or UnauthorizedAccessException) { WmiStatus = "WMI unavailable: " + ex.Message; }
        finally
        {
            if (rows is not null) Marshal.ReleaseComObject(rows);
            if (services is not null) Marshal.ReleaseComObject(services);
            if (locator is not null) Marshal.ReleaseComObject(locator);
        }
        return result;
    }

    [StructLayout(LayoutKind.Sequential)] private readonly struct POINT(int x, int y) { public readonly int X = x; public readonly int Y = y; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateDC(string driver, string device, string? output, IntPtr initData);
    [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr hdc, int index);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool EnumDisplayDevices(string? name, uint index, ref DISPLAY_DEVICE device, uint flags);
}
