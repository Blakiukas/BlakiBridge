using System.Reflection;
using System.Text.Json;

namespace BlakiBridge;

internal static class WelcomeCheck
{
    internal static void Run(string output)
    {
        var checks = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks.Add(name);
        }
        CheckBox Adjust(LiveMain main) => (CheckBox)typeof(LiveMain).GetField("adjust", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
        string Activity(LiveMain main) => ((Label)typeof(LiveMain).GetField("activityText", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!).Text;
        LiveMain Open(BridgeConfig config, bool hidden = false)
        {
            var main = new LiveMain(hidden, config) { Opacity = 0, ShowInTaskbar = false };
            main.Show(); Application.DoEvents();
            return main;
        }
        BridgeConfig? configured = null;
        foreach (string language in new[] { "EN", "LT", "RU" })
        {
            var config = new BridgeConfig { Language = language };
            using var main = Open(config);
            Check(main.GuideControl.Visible && !main.ScreensPage.Visible, language + " fresh launch opens guide");
            Check(!Adjust(main).Checked, language + " fresh launch leaves Adjust off");
            Check(!config.BridgeEnabled && Activity(main) == UiText.Pick("Off", "Išjungta", "Выключено"), language + " fresh launch leaves correction off");
            Check(config.WelcomeGuideShown == true, language + " welcome recorded");
            Check(main.GuideControl.Back.ForeColor == Color.FromArgb(255, 102, 102), language + " guide arrow red");
            Check(main.AboutBack.ForeColor != main.GuideControl.Back.ForeColor, language + " other back arrows unchanged");
            using var bitmap = new Bitmap(main.Width, main.Height);
            UiSnapshot.Paint(main, bitmap);
            bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, "welcome-" + language + ".png"));
            main.GuideControl.Back.PerformClick(); Application.DoEvents();
            Check(main.ScreensPage.Visible && !main.GuideControl.Visible, language + " back opens monitor layout");
            Check(!Adjust(main).Enabled, language + " Adjust disabled while correction off");
            Adjust(main).Checked = true;
            Check(!Adjust(main).Checked, language + " off state rejects adjustment");
            ((ToolStripMenuItem)main.TrayMenu.Items[1]).PerformClick();
            Check(Adjust(main).Enabled, language + " enabling correction unlocks Adjust");
            Adjust(main).Checked = true;
            Check(Adjust(main).Checked, language + " manual Adjust enabled");
            Check(Activity(main) == UiText.Pick("Enabled", "Įjungta", "Включено"), language + " enabled status wording");
            ((ToolStripMenuItem)main.TrayMenu.Items[1]).PerformClick();
            Check(Activity(main) == UiText.Pick("Off", "Išjungta", "Выключено"), language + " disabled status");
            Check(!Adjust(main).Checked && !Adjust(main).Enabled, language + " disabling ends adjustment");
            ((ToolStripMenuItem)main.TrayMenu.Items[1]).PerformClick();
            Check(!Adjust(main).Checked && Adjust(main).Enabled, language + " reenable does not resume adjustment");
            configured = config;
            main.Close();
        }
        string configPath = Path.Combine(Path.GetDirectoryName(output)!, "welcome-test-config.json");
        configured!.Save(configPath);
        var saved = BridgeConfig.Load(configPath);
        string calibration = JsonSerializer.Serialize(saved.Transitions);
        using (var reopened = Open(saved))
        {
            Check(reopened.ScreensPage.Visible && !reopened.GuideControl.Visible, "persisted second launch opens layout");
            Check(!Adjust(reopened).Checked, "persisted second launch leaves Adjust off");
            Check(JsonSerializer.Serialize(saved.Transitions) == calibration, "saved calibration preserved");
            reopened.ToggleGuide();
            Check(reopened.GuideControl.Back.ForeColor.R == 255, "manual guide retains red arrow");
            reopened.Close();
        }
        saved.WelcomeGuideShown = null;
        using (var upgraded = Open(saved))
        {
            Check(upgraded.ScreensPage.Visible && !upgraded.GuideControl.Visible, "legacy configured upgrade does not show welcome");
            Check(JsonSerializer.Serialize(saved.Transitions) == calibration, "legacy calibration preserved");
            upgraded.Close();
        }
        var freshHidden = new BridgeConfig();
        using (var hidden = Open(freshHidden, true))
        {
            Check(!hidden.Visible && freshHidden.WelcomeGuideShown == false, "fresh Windows startup defers welcome");
            Check(!Adjust(hidden).Checked, "hidden startup leaves Adjust off");
            freshHidden.Save(configPath);
            hidden.Close();
        }
        using (var pending = Open(BridgeConfig.Load(configPath)))
        {
            Check(pending.GuideControl.Visible, "deferred welcome survives restart with generated maps");
            pending.Close();
        }
        var trayConfig = new BridgeConfig();
        using (var hidden = Open(trayConfig, true))
        {
            hidden.Show(); Application.DoEvents();
            Check(hidden.GuideControl.Visible && trayConfig.WelcomeGuideShown == true, "opening tray after hidden start shows welcome");
            hidden.Close();
        }
        File.WriteAllText(output, JsonSerializer.Serialize(new { passed = true, count = checks.Count, checks }));
    }
}
