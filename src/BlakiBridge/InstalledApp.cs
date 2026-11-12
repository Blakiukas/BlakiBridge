using Microsoft.Win32;
namespace BlakiBridge;
internal static class InstalledApp
{
    const string Key=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BlakiBridge_is1";
    static string? Uninstaller {get{using var key=Registry.CurrentUser.OpenSubKey(Key);var location=key?.GetValue("InstallLocation") as string;if(string.IsNullOrWhiteSpace(location)||!string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(location)),Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),StringComparison.OrdinalIgnoreCase))return null;string path=Path.Combine(location,"unins000.exe");return File.Exists(path)?path:null;}}
    internal static bool CanUninstall=>PackageMode.IsPackaged||Uninstaller!=null;
    internal static void Uninstall(){if(PackageMode.IsPackaged){System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:appsfeatures"){UseShellExecute=true});return;}if(Uninstaller is string path)System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});}
}
internal sealed class SetupExitRegistration:IDisposable
{
    readonly RegisteredWaitHandle wait;
    internal SetupExitRegistration(EventWaitHandle signal,LiveMain main){wait=ThreadPool.RegisterWaitForSingleObject(signal,(_,_)=>{try{main.BeginInvoke((Action)(()=>main.ExitForSetup()));}catch(InvalidOperationException){}},null,Timeout.Infinite,false);}
    public void Dispose()=>wait.Unregister(null);
}
