using System.Diagnostics;
try
{
    Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"BlakiBridge.exe"),"--startup")
    {UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
}
catch(Exception ex)
{
    var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BlakiBridge","diagnostics");
    try{Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"startup-error.txt"),DateTime.UtcNow.ToString("O")+" "+ex.Message);}catch{}
}
