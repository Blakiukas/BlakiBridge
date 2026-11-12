using System.Runtime.InteropServices;
namespace BlakiBridge;
internal static class PackageMode
{
    internal static bool IsPackaged {get{uint size=0;return GetCurrentPackageFullName(ref size,IntPtr.Zero)==122;}}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern int GetCurrentPackageFullName(ref uint length,IntPtr name);
}
#if STORE_PACKAGE
public sealed partial class LiveMain
{
    private bool startupBusy;
    private void ConfigurePackageStartup()
    {
        startup.Enabled=false;
        if(diagnostics || !PackageMode.IsPackaged)return;
        async Task Refresh()
        {
            if(startupBusy)return;startupBusy=true;
            try{var task=await Windows.ApplicationModel.StartupTask.GetAsync("BlakiBridgeStartup");updating=true;startup.Checked=task.State is Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;updating=false;startup.Enabled=true;}
            catch(Exception ex){BridgeTrace.Write("package_startup_read_failed",new{error=ex.Message});}
            finally{updating=false;startupBusy=false;}
        }
        Shown+=async(_,_)=>await Refresh();Activated+=async(_,_)=>await Refresh();
        startup.CheckedChanged+=async(_,_)=>
        {
            if(updating||startupBusy)return;startupBusy=true;startup.Enabled=false;
            try
            {
                var task=await Windows.ApplicationModel.StartupTask.GetAsync("BlakiBridgeStartup");
                if(startup.Checked){var state=await task.RequestEnableAsync();if(state is not (Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy))MessageBox.Show(this,UiText.Pick("Enable BlakiBridge in Windows Settings → Apps → Startup.","Įjunkite BlakiBridge Windows nustatymuose → Programos → Paleistis.","Включите BlakiBridge в параметрах Windows → Приложения → Автозагрузка."),"BlakiBridge");}
                else task.Disable();
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message,"BlakiBridge");}
            finally{startupBusy=false;await Refresh();}
        };
    }
}
#endif
