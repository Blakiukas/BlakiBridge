namespace BlakiBridge;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--cursor-restore-watch" && int.TryParse(args[1], out int owner))
        { try { CursorRestoreGuard.Watch(owner); } catch { } return; }
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--input-continuity-test")
        { InputContinuityCheck.Run(args[1]); return; }
        if(args.Length==2 && args[0]=="--release-check")
        {try{ReleaseCheck.Run(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--appearance-check")
        {try{AppearancePreview.Capture(args[1]);}catch(Exception ex){Directory.CreateDirectory(args[1]);File.WriteAllText(Path.Combine(args[1],"error.txt"),ex.ToString());Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--appearance-preview")
        {var config=BridgeConfig.Load(args[1]);config.BridgeEnabled=false;using var preview=new LiveMain(diagnosticConfig:config);preview.Text="BlakiBridge - Appearance preview";Application.Run(preview);return;}
        if(args.Length==2 && args[0]=="--monitor-identity-test")
        {try{MonitorIdentityCheck.Run(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--welcome-ui-test")
        {try{WelcomeCheck.Run(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--reliability-ui-test")
        {try{ReliabilityCheck.Run(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--export-store-assets"){BrandGlyph.ExportStoreAssets(args[1]);return;}
        if(args.Length==2 && args[0]=="--export-icon"){BrandGlyph.ExportIcon(args[1]);return;}
        if(args.Length==2 && args[0]=="--navigation-ui-test"){ResizeCheck.Navigation(args[1]);return;}
        if(args.Length==2 && args[0]=="--resize-ui-test"){ResizeCheck.Run(args[1]);return;}
        if (args.Length == 2 && args[0] == "--speed-probe")
        { try { SpeedProbe.Run(args[1]); } catch(Exception ex) { File.WriteAllText(args[1], ex.ToString()); Environment.ExitCode = 1; } return; }
        if (args.Length == 1 && args[0] == "--cursor-redraw-probe")
        {
            BridgeTrace.Enabled = true;
            if (CursorRestoreGuard.Ensure()) CursorRedraw.Probe();
            // Give the background diagnostic writer a chance to flush; no pointer displacement.
            System.Threading.Thread.Sleep(200);
            return;
        }
        if(args.Length==1 && args[0]=="--layout-preview")
        {
            var previewConfig=BridgeConfig.Load(); previewConfig.BridgeEnabled=false;
            using var preview=new LiveMain(diagnosticConfig:previewConfig);
            preview.Text="BlakiBridge · UI check";
            preview.StartPosition=FormStartPosition.Manual;
            preview.Location=Screen.PrimaryScreen!.WorkingArea.Location+new Size(80,80);
            preview.Shown+=(_,_)=>{preview.Size=new Size(1440,940);preview.EditDisplay(0);};
            Application.Run(preview); return;
        }
        if(args.Length==2 && args[0]=="--embedded-gap-ui-test")
        {try{SelfTest.EmbeddedGapUi(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--input-ui-test")
        {try{SelfTest.InputUi(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--scale-ui-test")
        {try{SelfTest.ScaleUi(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--topology-ui-test")
        {try{SelfTest.TopologyUi(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--layout-ui-test")
        {try{SelfTest.LayoutUi(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if(args.Length==2 && args[0]=="--dpi-ui-test")
        {try{SelfTest.DpiUi(args[1]);}catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{error=ex.ToString()}));Environment.ExitCode=1;}return;}
        if (args.Length == 2 && args[0] == "--gap-ui-test")
        {
            try { SelfTest.GapUi(args[1]); }
            catch (Exception ex) { File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(new { error = ex.ToString() })); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--resource-ui-test")
        {
            try { SelfTest.ResourceUi(args[1]); }
            catch (Exception ex) { File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(new { error = ex.ToString() })); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--self-test" or "--visual-test")
        {
            try { SelfTest.Run(args[1], visual: args[0] == "--visual-test"); }
            catch (Exception ex) { File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(new { error = ex.ToString() })); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--stability-ui-test")
        {
            try { SelfTest.StabilityUi(args[1]); }
            catch (Exception ex) { File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(new { error = ex.ToString() })); Environment.ExitCode = 1; }
            return;
        }
        using var activateRequest=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\BlakiBridge.OpenWindow");
        using var instance = new Mutex(true, "Local\\BlakiBridge.Desktop", out bool created);
        if (!created)
        {
            if(!args.Contains("--startup"))
            {
                foreach(var process in System.Diagnostics.Process.GetProcessesByName("BlakiBridge"))
                {
                    try{if(process.Id!=Environment.ProcessId && process.MainModule?.FileName==Environment.ProcessPath)AllowSetForegroundWindow(process.Id);}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}finally{process.Dispose();}
                }
                activateRequest.Set();
            }
            return;
        }
        BridgeTrace.Enabled = true;
        BridgeTrace.Write("process_start");
        using var main=new LiveMain(args.Contains("--startup"));
        using var setupExit=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\BlakiBridge.SetupExit");
        using var setupRegistration=new SetupExitRegistration(setupExit,main);
        RegisteredWaitHandle? registration=null;
        main.Shown+=(_,_)=>registration=ThreadPool.RegisterWaitForSingleObject(activateRequest,(_,_)=>
        {
            if(main.IsDisposed || !main.IsHandleCreated)return;
            try{main.BeginInvoke((Action)(()=>{if(main.IsDisposed)return;main.Show();if(main.WindowState==FormWindowState.Minimized)main.WindowState=FormWindowState.Normal;main.Activate();}));}
            catch(InvalidOperationException){}
        },null,Timeout.Infinite,false);
        try{Application.Run(main);}finally{registration?.Unregister(null);}
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(int processId);
}

