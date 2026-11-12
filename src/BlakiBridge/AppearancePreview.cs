using System.Text.Json;
namespace BlakiBridge;
internal static class AppearancePreview
{
    internal static void Capture(string directory)
    {
        Directory.CreateDirectory(directory);
        var config=BridgeConfig.Load(Path.Combine(directory,"preview-config.json"));
        config.BridgeEnabled=false;
        using var form=new LiveMain(diagnosticConfig:config){ShowInTaskbar=false,Opacity=0};
        form.Show();Application.DoEvents();
        object[] Geometry(Control root)=>root.Controls.Cast<Control>().Select(c=>(object)new{type=c.GetType().Name,c.Text,c.Bounds,children=Geometry(c)}).ToArray();
        var records=new List<object>();
        void Capture(string name){Application.DoEvents();using var bitmap=new Bitmap(form.Width,form.Height);UiSnapshot.Paint(form,bitmap);bitmap.Save(Path.Combine(directory,name+".png"));}
        foreach(var language in new[]{"EN","LT","RU"})
        foreach(var size in new[]{new Size(960,660),new Size(1440,940)})
        {
            form.SelectLanguage(language);form.Size=size;Application.DoEvents();
            Capture($"main-{language}-{size.Width}");
            records.Add(new{language,size,dpi=form.DeviceDpi,controls=Geometry(form)});
            form.ToggleGuide();Capture($"guide-{language}-{size.Width}");
            records.Add(new{language,size,dpi=form.DeviceDpi,controls=Geometry(form)});
            form.ToggleGuide();
        }
        form.EditDisplay(0);Capture("dimensions");
        form.ToggleAbout();Capture("about");
        form.ToggleGuide();Capture("guide");
        File.WriteAllText(Path.Combine(directory,"geometry.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
        form.Close();
    }
}
