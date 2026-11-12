using System.Text.Json;

namespace BlakiBridge;

internal static class SelfTest
{
    public static void InputUi(string output)
    {
        int checks=0;void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);checks++;}
        foreach(var monitor in MonitorModel.Detect().Where(m=>m.Bounds.Height>=660))
        {
            using var form=new Form{Opacity=0,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=monitor.Bounds.Location+new Size(40,40),Size=new Size(350,200)};
            using var number=new RoundedNumber{Location=new Point(20,20),DecimalPlaces=1,Minimum=25,Maximum=300,Value=100,Increment=5};
            using var other=new Button{Location=new Point(20,100),Text="focus"};form.Controls.AddRange([number,other]);form.Show();other.Focus();Application.DoEvents();
            var clip=number.Controls.OfType<Panel>().Single();var editor=clip.Controls.OfType<NumericUpDown>().Single();var readout=clip.Controls.Cast<Control>().Single(c=>c.GetType().Name=="NumberReadout");
            Check(readout.Visible,"Idle smooth number visible");
            using(var image=new Bitmap(number.Width,number.Height))
            {number.DrawToBitmap(image,number.ClientRectangle);using(var overlay=new Bitmap(readout.Width,readout.Height)){readout.DrawToBitmap(overlay,readout.ClientRectangle);using var g=Graphics.FromImage(image);var point=number.PointToClient(readout.PointToScreen(Point.Empty));g.DrawImageUnscaled(overlay,point);}image.Save(Path.Combine(Path.GetDirectoryName(output)!,"number-"+form.DeviceDpi+".png"));}
            editor.Focus();Application.DoEvents();Check(readout.Visible && editor.ContainsFocus,"Smooth editing on focus");
            editor.Text="125.5";editor.UpButton();Check(number.Value==130.5m,"Typed decimal and increment preserved");
            var textBox=editor.Controls.OfType<TextBox>().Single();textBox.SelectAll();Check(textBox.SelectedText==editor.Text,"Select-all preserved");textBox.SelectedText="140.5";editor.UpButton();Check(number.Value==145.5m,"Selection replacement preserves decimal input");
            Check(readout.Visible,"Editing never swaps text renderer");
            other.Focus();Application.DoEvents();Check(readout.Visible,"Readout returns after blur");
            number.Value=299;
            typeof(RoundedNumber).GetMethod("OnMouseWheel",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(number,[new HandledMouseEventArgs(MouseButtons.None,0,0,0,120)]);
            Check(number.Value==300,"Wheel clamps upper bound");number.Value=25;
            typeof(RoundedNumber).GetMethod("OnMouseWheel",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(number,[new HandledMouseEventArgs(MouseButtons.None,0,0,0,-120)]);
            Check(number.Value==25,"Wheel clamps lower bound");
            readout.GetType().GetMethod("OnMouseDown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(readout,[new MouseEventArgs(MouseButtons.Left,1,2,2,0)]);
            Check(editor.ContainsFocus && readout.Visible,"Click enters smooth editing");form.Close();
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{passed=true,checks}));
    }
    public static void ScaleUi(string output)
    {
        var results=new List<object>();var issues=new List<string>();
        using var main=new LiveMain(diagnosticConfig:new BridgeConfig{Language="LT"}){Opacity=0,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual};
        var screens=MonitorModel.Detect().Where(m=>m.Bounds.Height>=660).ToArray();
        main.Location=screens[0].Bounds.Location+new Size(30,30);main.Show();Application.DoEvents();
        for(int cycle=0;cycle<3;cycle++)foreach(var monitor in screens)
        {
            main.Location=monitor.Bounds.Location+new Size(30,30);Application.DoEvents();main.Size=main.MinimumSize;Application.DoEvents();
            var expected=new Size((int)(960*main.DeviceDpi/96f),(int)(660*main.DeviceDpi/96f));
            if(main.MinimumSize!=expected)issues.Add($"{cycle}/{main.DeviceDpi}: minimum {main.MinimumSize} expected {expected}");
            if(Math.Abs(main.Font.SizeInPoints-11*main.DeviceDpi/96f)>.05)issues.Add($"{cycle}/{main.DeviceDpi}: font {main.Font.SizeInPoints}");
            main.EditDisplay(0);Application.DoEvents();
            results.Add(new{cycle,dpi=main.DeviceDpi,main.Size,main.MinimumSize,font=main.Font.SizeInPoints,editorFont=main.DisplayEditor!.Font.SizeInPoints,options=main.OptionBandHeight,footer=main.FooterBandHeight});
            using var bmp=new Bitmap(main.Width,main.Height);UiSnapshot.Paint(main,bmp);bmp.Save(Path.Combine(Path.GetDirectoryName(output)!,"scale-"+cycle+"-"+main.DeviceDpi+".png"));
            main.DisplayEditor.Back.PerformClick();
        }
        main.Close();File.WriteAllText(output,JsonSerializer.Serialize(new{passed=issues.Count==0,issues,results}));if(issues.Count>0)Environment.ExitCode=1;
    }
    public static void LayoutUi(string output)
    {
        var issues = new List<string>(); var geometry = new List<object>();
        void Inspect(Control root, string path)
        {
            if(root is LayoutRow)
            {
                int centre=root.Padding.Top+(root.ClientSize.Height-root.Padding.Vertical)/2;
                foreach(Control item in root.Controls)
                    if(item.Visible && Math.Abs(item.Top+item.Height/2-centre)>1)issues.Add(path+" row centre mismatch "+item.Text);
            }
            if (root is ScrollableControl scroll && (scroll.HorizontalScroll.Visible || scroll.VerticalScroll.Visible)) issues.Add(path+" scrollbar");
            foreach(Control child in root.Controls)
            {
                if (!child.Visible) continue;
                string name=path+"/"+child.GetType().Name+" "+child.Text.Replace("\n"," ");
                if(root.Parent is not RoundedNumber and not ChoiceField && !root.ClientRectangle.Contains(child.Bounds)) issues.Add(name+" outside: "+child.Bounds+" parent "+root.ClientRectangle);
                if(child is Label label && !label.AutoSize && !string.IsNullOrEmpty(label.Text))
                {
                    var measure=TextRenderer.MeasureText(label.Text,label.Font,new Size(Math.Max(1,label.Width-label.Padding.Horizontal),int.MaxValue),TextFormatFlags.WordBreak);
                    if(measure.Height>label.Height-label.Padding.Vertical+2)issues.Add(name+" text height "+measure.Height+" available "+(label.Height-label.Padding.Vertical));
                }
                Inspect(child,name);
            }
        }
        foreach(var monitor in MonitorModel.Detect())
        foreach(string lang in new[]{"EN","LT","RU"})
        {
            using var main=new LiveMain(diagnosticConfig:new BridgeConfig{Language=lang,OptimizeMouseSpeed=true,WelcomeGuideShown=true});
            main.StartPosition=FormStartPosition.Manual;main.Location=new Point(monitor.Bounds.Left+40,monitor.Bounds.Top+40);
            main.ShowInTaskbar=false;main.Opacity=0;main.Show();Application.DoEvents();
            main.Size=main.MinimumSize;Application.DoEvents();
            string key=lang+"-"+main.DeviceDpi;
            void Capture(string page)
            {
                Application.DoEvents(); Inspect(main,key+" "+page);
                if(main.OptionBandHeight!=main.FooterBandHeight)issues.Add(key+" unequal bands");
                if(main.ScreensPage.Visible)
                {
                    var canvas=main.CanvasControl;var area=main.ScreensPage;
                    if(canvas.Left!=area.ClientSize.Width-canvas.Right || area.ClientSize.Height-canvas.Bottom!=area.Padding.Bottom)issues.Add(key+" canvas margins");
                }
                using var bmp=new Bitmap(main.Width,main.Height);UiSnapshot.Paint(main,bmp);
                bmp.Save(Path.Combine(Path.GetDirectoryName(output)!,key+"-"+page+".png"));
                geometry.Add(new{key,page,main.Size,main.MinimumSize,workspace=main.ContentArea.Bounds,canvas=main.CanvasControl.Bounds,top=main.OptionBandHeight,bottom=main.FooterBandHeight});
            }
            Capture("main");main.ToggleGuide();Capture("guide");main.ToggleAbout();Capture("about");
            main.EditDisplay(0);Capture("dimensions");main.DisplayEditor!.Preview.SelectParameter(1);Capture("measurement");main.DisplayEditor!.SelectTab(1);Capture("speed");main.DisplayEditor.SelectTab(0);
            for(int i=0;i<12;i++){main.Size=new Size(main.MinimumSize.Width+(i%2)*170,main.MinimumSize.Height+(i%2)*90);Application.DoEvents();main.Refresh();}
            main.Size=main.MinimumSize;Capture("resized");main.DisplayEditor.Close();
            main.EditDisplay(0);main.DisplayEditor!.SelectTab(2);Capture("connections");
            if(main.DisplayEditor.ConnectionsDraft is null || !main.DisplayEditor.ConnectionsDraft.Visible)issues.Add(key+" third tab missing");
            main.DisplayEditor.Back.PerformClick();
            main.Close();
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{passed=issues.Count==0,issues,geometry}));
        if(issues.Count>0)Environment.ExitCode=1;
    }
    public static void EmbeddedGapUi(string output)
    {
        int passed=0;void Check(bool test,string description){if(!test)throw new InvalidOperationException(description);passed++;}
        var config=new BridgeConfig{BridgeEnabled=false};
        using var main=new LiveMain(diagnosticConfig:config){Opacity=0,ShowInTaskbar=false};
        main.StartPosition=FormStartPosition.Manual;main.Location=Screen.PrimaryScreen!.Bounds.Location;main.Show();Application.DoEvents();
        Check(config.Transitions.Count>0,"Detected pairs");
        main.SelectGapMode(true);Application.DoEvents();
        Check(main.GapEditor is {TopLevel:false} && !config.UsePhysicalGap,"Setup embedded and inactive until complete");
        main.GapEditor!.Back.PerformClick();Application.DoEvents();
        Check(main.GapEditor==null && !config.UsePhysicalGap && config.Gaps.Count==0,"Back cancels missing setup without saving");
        main.SelectGapMode(true);Application.DoEvents();
        int count=0;
        while(main.GapEditor is {} current && count++<10)
        {
            current.SetDistance(20+count);
            ((Button)current.AcceptButton!).PerformClick();Application.DoEvents();
        }
        Check(count==config.Transitions.Count && main.GapEditor==null && config.UsePhysicalGap,"Every pair completes before enabling");
        string saved=JsonSerializer.Serialize(config.Gaps);var pair=config.Transitions[0];
        main.EditGap(pair);Application.DoEvents();main.GapEditor!.SetDistance(99);main.ToggleGuide();Application.DoEvents();
        Check(main.GapEditor==null && saved==JsonSerializer.Serialize(config.Gaps),"Guide navigation cancels pending distance");
        main.EditGap(pair);Application.DoEvents();main.GapEditor!.SetDistance(37);((Button)main.GapEditor.AcceptButton!).PerformClick();Application.DoEvents();
        Check(config.Gaps[GapSettings.Key(pair.A,pair.B)].EdgeDistanceMm==37,"Save commits selected gap");
        main.SelectGapMode(false);Check(!config.UsePhysicalGap && config.Gaps.Count==count,"Disable retains distances");
        main.Close();File.WriteAllText(output,JsonSerializer.Serialize(new{passed}));
    }
    public static void TopologyUi(string output)
    {
        int passed=0;void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);passed++;}
        var monitors=MonitorModel.Detect();Check(monitors.Count>=3,"Three-screen regression fixture");
        var c=new BridgeConfig{BridgeEnabled=true,OptimizeMouseSpeed=true};
        c.DiagramPositions[monitors[0].DeviceName]=new(-225,-47);c.DiagramWidths[monitors[0].DeviceName]=121;
        c.DiagramPositions[monitors[1].DeviceName]=new(29,109);c.DiagramWidths[monitors[1].DeviceName]=263;
        c.DiagramPositions[monitors[2].DeviceName]=new(-98,-44);c.DiagramWidths[monitors[2].DeviceName]=526;
        c.Transitions.Add(new(){A=monitors[2].DeviceName,B=monitors[1].DeviceName,Side=Edge.Top,SizeA=monitors[2].Bounds.Size,SizeB=monitors[1].Bounds.Size,StartA=0,EndA=100,StartB=0,EndB=100});
        using var main=new LiveMain(diagnosticConfig:c){Opacity=0,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=Screen.PrimaryScreen!.Bounds.Location};main.Show();Application.DoEvents();
        Check(c.Connections?.Count==2,"Two physical neighbors, independent of old pixel positions");
        bool Has(string a,string b,Edge side)=>c.Transitions.Any(t=>(t.A==a && t.B==b && t.Side==side)||(t.B==a && t.A==b && t.Side==Transition.Opposite(side)));
        Check(Has(monitors[0].DeviceName,monitors[2].DeviceName,Edge.Right),"Left screen connects right to large screen");
        Check(Has(monitors[2].DeviceName,monitors[1].DeviceName,Edge.Bottom),"Large screen connects downward to small screen");
        Check(c.TopologyReviewRequired && !c.BridgeEnabled,"Wrong legacy calibration does not silently start");
        Check(c.Transitions.All(t=>t.Valid),"Initial physical overlap intervals valid");
        string before=JsonSerializer.Serialize(c.Transitions);c.Transitions[0].StartA+=1;before=JsonSerializer.Serialize(c.Transitions);
        main.CanvasControl.DiagramPositions[monitors[0].DeviceName]=new(-500,400);
        var rebuilt=MonitorTopology.Build(main.CanvasControl,c.Connections!,c.Transitions);
        Check(before==JsonSerializer.Serialize(rebuilt),"Visual move preserves saved pairs and calibrated endpoints");
        main.CanvasControl.Refresh();Check(main.CanvasControl.GapBadges.Count==0 && main.CanvasControl.GapAt(new Point(10,10))==null,"Main canvas has no gap hit targets");
        main.EditDisplay(0);main.DisplayEditor!.SelectTab(2);Application.DoEvents();
        Check(main.DisplayEditor.ConnectionsDraft is {Visible:true},"Physical gap is third settings tab");
        var draft=main.DisplayEditor.ConnectionsDraft!;draft.Links.Clear();main.DisplayEditor.Back.PerformClick();Application.DoEvents();Check(c.Connections!.Count==2,"Back cancels draft neighbor edits");
        main.EditDisplay(0);main.DisplayEditor!.SelectTab(2);Application.DoEvents();
        draft=main.DisplayEditor.ConnectionsDraft!;
        var choices=draft.Controls.Cast<Control>().SelectMany(Descendants).OfType<ComboBox>().ToArray();
        Check(choices.Length==2,"Neighbor and side choices available");
        // Saving an unchanged existing pair retains the calibration exactly.
        ((Button)main.DisplayEditor.AcceptButton!).PerformClick();Application.DoEvents();
        Check(before==JsonSerializer.Serialize(c.Transitions),"Unchanged pair save preserves calibration");
        main.EditDisplay(0);main.DisplayEditor!.SelectTab(1);main.DisplayEditor.SpeedInput.Value=125;main.DisplayEditor.Back.PerformClick();
        Check(!c.MouseSpeedPreview.Any(),"Back removes live speed preview");
        main.EditDisplay(0);main.DisplayEditor!.SelectTab(2);draft=main.DisplayEditor.ConnectionsDraft!;
        choices=draft.Controls.Cast<Control>().SelectMany(Descendants).OfType<ComboBox>().ToArray();
        choices.Single(x=>x.AccessibleName==UiText.Pick("Position","Vieta","Положение")).SelectedIndex=(int)Edge.Bottom;
        ((Button)main.DisplayEditor.AcceptButton!).PerformClick();Application.DoEvents();
        Check(Has(monitors[0].DeviceName,monitors[2].DeviceName,Edge.Bottom),"Manual side overrides inferred side");
        Check(c.TopologyReviewRequired && !c.BridgeEnabled,"Changed side requires calibration review");
        foreach(var map in c.Transitions)
        foreach(bool reverse in new[]{false,true})
        foreach(bool end in new[]{false,true})
        {
            var monitor=monitors.First(m=>m.DeviceName==(reverse?map.B:map.A));var edge=reverse?Transition.Opposite(map.Side):map.Side;
            int value=reverse?(end?map.EndB:map.StartB):(end?map.EndA:map.StartA);
            using var marker=new MarkerHandle(monitor.Bounds,edge,Color.Orange,"test",()=>value,_=>{},_=>{},()=>{},Math.Max(1,monitor.DpiX/96f)){Opacity=0};
            marker.Show();marker.Place();Application.DoEvents();
            using(var markerBitmap=new Bitmap(marker.Width,marker.Height)){marker.DrawToBitmap(markerBitmap,new Rectangle(Point.Empty,markerBitmap.Size));markerBitmap.Save(Path.Combine(Path.GetDirectoryName(output)!,"marker-"+monitor.DpiX+"-"+edge+".png"));}
            Check(monitor.Bounds.Contains(marker.Bounds),"Actual-DPI marker within selected screen");
            Check(edge switch{Edge.Left=>marker.Left==monitor.Bounds.Left,Edge.Right=>marker.Right==monitor.Bounds.Right,Edge.Top=>marker.Top==monitor.Bounds.Top,_=>marker.Bottom==monitor.Bounds.Bottom},"Marker uses saved physical edge");
        }
        var roundtrip=JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(c))!;
        Check(roundtrip.Connections!.Count==2,"Explicit topology persists");
        main.Close();File.WriteAllText(output,JsonSerializer.Serialize(new{passed,monitors=monitors.Select(m=>new{m.DeviceName,m.ModelName})}));
        static IEnumerable<Control> Descendants(Control c){yield return c;foreach(Control child in c.Controls)foreach(var nested in Descendants(child))yield return nested;}
    }
    public static void DpiUi(string output)
    {
        var results=new List<object>();
        foreach(var monitor in MonitorModel.Detect())
        {
            using var main=new LiveMain(diagnosticConfig:new BridgeConfig());
            main.StartPosition=FormStartPosition.Manual;main.Location=new Point(monitor.Bounds.Left+40,monitor.Bounds.Top+40);
            main.ShowInTaskbar=false;main.Opacity=0;main.Show();Application.DoEvents();
            main.EditDisplay(0);Application.DoEvents();
            var editor=main.DisplayEditor!;
            if(main.OptionBandHeight!=main.FooterBandHeight || editor.Bounds!=main.ContentArea.ClientRectangle)throw new InvalidOperationException("DPI page/band mismatch");
            using(var bitmap=new Bitmap(main.Width,main.Height)){main.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!,"dpi-"+monitor.DeviceName.Replace("\\","").Replace(".","")+".png"));}
            results.Add(new{monitor.DeviceName,expectedDpi=monitor.DpiX,actualDpi=main.DeviceDpi,main.Bounds,fontPoints=main.Font.Size,editorDpi=editor.DeviceDpi,editorSize=editor.Size,buttons=editor.ActionBar.Controls.Cast<Control>().Select(c=>new{c.Text,c.Size,font=c.Font.Size,preferred=c.GetPreferredSize(Size.Empty)}).ToArray(),optionBand=main.OptionBandHeight,footerBand=main.FooterBandHeight});
            editor.Close();main.Close();
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{passed=true,results}));
    }
    public static void GapUi(string output)
    {
        int passed = 0;
        var languageTimings = new List<double>();
        var pageGeometry = new List<object>();
        void Check(bool value, string description) { if (!value) throw new InvalidOperationException(description); passed++; }
        void Offscreen(Form form)
        {
            form.StartPosition = FormStartPosition.Manual; form.Location = new Point(SystemInformation.VirtualScreen.Right + 5000, SystemInformation.VirtualScreen.Bottom + 5000);
            form.ShowInTaskbar = false; form.Opacity = 0; form.Show(); Application.DoEvents();
        }
        void Capture(Form form, string name)
        {
            using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
            image.Save(Path.Combine(Path.GetDirectoryName(output)!, name + ".png"));
        }
        void ContainsControls(Control parent)
        {
            foreach (Control child in parent.Controls) if (child.Visible) {
                if(parent.Parent is RoundedNumber && child is NumericUpDown) { Check(child.Left == 0 && child.Top == 0 && child.Height == parent.Height, "Native number text stays inside deliberately clipped viewport"); continue; }
                Check(parent.ClientRectangle.Contains(child.Bounds), "Child clipped: " + child.Text); ContainsControls(child); }
        }
        using (var number = new RoundedNumber { Minimum = 0, Maximum = 300, DecimalPlaces = 1, Value = 100 })
        {
            var native = number.Controls.OfType<Panel>().Single().Controls.OfType<NumericUpDown>().Single();
            native.Text = 123.4m.ToString(System.Globalization.CultureInfo.CurrentCulture); native.UpButton();
            Check(number.Value == 124.4m, "Rounded field commits typed decimal before arrow increment");
            native.DownButton(); Check(number.Value == 123.4m, "Rounded field preserves decimal arrow decrement");
            number.Value = 300; native.UpButton(); Check(number.Value == 300, "Rounded field retains numeric upper bound");
            number.Value = 0; native.DownButton(); Check(number.Value == 0, "Rounded field retains numeric lower bound");
        }
        var config = new BridgeConfig();
        using (var toggle = new BridgeSwitch { AutoSize = false, Size = new Size(230, 36), BackColor = Color.FromArgb(34,34,34), ForeColor = Color.White })
        using (var reused = new Bitmap(230,36))
        using (var fresh = new Bitmap(230,36))
        {
            var paint = typeof(BridgeSwitch).GetMethod("OnPaint", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            void Render(Bitmap image) { using var g = Graphics.FromImage(image); paint.Invoke(toggle, [new PaintEventArgs(g, toggle.ClientRectangle)]); }
            toggle.Text = "Very long earlier label that must disappear"; toggle.Checked = true; Render(reused);
            toggle.Text = "Enabled"; toggle.Checked = false; Render(reused); Render(fresh);
            bool identical = true;
            for (int y=0;y<36;y++) for(int x=0;x<230;x++) if(reused.GetPixel(x,y) != fresh.GetPixel(x,y)) identical=false;
            Check(identical, "Repeated switch painting erases previous label and state completely");
        }
        using (var selector = new LanguageBox())
        using (var reusedSelector = new Bitmap(96,32))
        using (var freshSelector = new Bitmap(96,32))
        {
            int changes = 0; selector.SelectedIndexChanged += (_, _) => changes++;
            var paintSelector = typeof(LanguageBox).GetMethod("OnPaint", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            void RenderSelector(Bitmap image) { using var g = Graphics.FromImage(image); paintSelector.Invoke(selector, [new PaintEventArgs(g, selector.ClientRectangle)]); }
            foreach (string code in new[] { "LT", "RU", "EN", "RU", "LT", "EN" })
            {
                selector.SelectedItem = code; RenderSelector(reusedSelector); RenderSelector(freshSelector);
                bool identical = true;
                for (int y=0;y<32;y++) for(int x=0;x<96;x++) if(reusedSelector.GetPixel(x,y) != freshSelector.GetPixel(x,y)) identical=false;
                Check(identical, "Selector completely repaints transition to " + code);
                Check(selector.Text == code && selector.AccessibleDescription!.Contains(code), "Visible and accessible language agree");
                Check(reusedSelector.GetPixel(92,16).GetBrightness() < 0.3f, "Selector arrow area stays dark");
            }
            selector.SelectedItem = "invalid"; selector.SelectedItem = "EN";
            Check(changes == 6 && selector.SelectedItem == "EN", "Invalid or repeated language does not commit a change");
        }
        var earlyConfig = new BridgeConfig { DiagramWidths = new() { ["retained"] = 345 } };
        using (var early = new LiveMain(diagnosticConfig: earlyConfig))
        {
            typeof(LiveMain).GetMethod("Save", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(early, null);
            Check(earlyConfig.DiagramWidths.GetValueOrDefault("retained") == 345, "Uninitialized window cannot erase saved dimensions");
        }
        using (var preview = new MonitorPreview { Size = new Size(500, 380), BackColor = Color.FromArgb(25,25,25) })
        using (var bitmap = new Bitmap(500, 380))
        {
            var zero = new DisplayMeasurements { Width = 500, Height = 300 };
            preview.SetMeasurements(zero);
            for (int edge = 2; edge <= 5; edge++)
            {
                preview.SelectParameter(edge); preview.DrawToBitmap(bitmap, preview.ClientRectangle);
                var highlightBounds = preview.HighlightBounds;
                Check(highlightBounds.Width > 0 && highlightBounds.Height > 0, "Zero bezel has visible highlight " + edge);
                Check(preview.Caption.Contains("0 mm"), "Zero bezel stays zero " + edge);
                bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, "zero-bezel-" + edge + ".png"));
            }
        }
        using var main = new LiveMain(diagnosticConfig: config); Offscreen(main);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) Check(main.DarkFrameAttribute() == (SystemInformation.HighContrast ? 0 : 1), "Native frame opts into system dark theme");
        var canvas = main.CanvasControl;
        if (canvas.Monitors.Count >= 2)
        {
            var original = new Dictionary<string, PointF>(canvas.DiagramPositions);
            for (int i = 0; i < canvas.Monitors.Count; i++) canvas.DiagramPositions[canvas.Monitors[i].DeviceName] = new PointF(i * 10000, 0);
            var a = canvas.Monitors[0]; var b = canvas.Monitors[1];
            canvas.DiagramPositions[b.DeviceName] = new PointF(canvas.DisplaySize(a).Width, 0);
            Check(!canvas.HasOverlap(0), "Touching edges allowed");
            canvas.DiagramPositions[b.DeviceName] = new PointF(0, 0);
            Check(canvas.HasOverlap(0) && canvas.HasOverlap(1), "Full overlap detected in either direction");
            canvas.DiagramPositions.Clear(); foreach (var pair in original) canvas.DiagramPositions.Add(pair.Key, pair.Value);
        }
        Check(main.TrayMenu.Items.Count == 5, "Tray has exactly five requested entries");
        var trayEnabled = (ToolStripMenuItem)main.TrayMenu.Items[1];
        var trayOptimize = (ToolStripMenuItem)main.TrayMenu.Items[2];
        var trayGap = (ToolStripMenuItem)main.TrayMenu.Items[3];
        bool priorEnabled = config.BridgeEnabled, priorOptimize = config.OptimizeMouseSpeed;
        trayEnabled.PerformClick(); Check(trayEnabled.Checked == config.BridgeEnabled && config.BridgeEnabled != priorEnabled, "Tray enable commits and checks");
        trayEnabled.PerformClick(); Check(config.BridgeEnabled == priorEnabled && trayEnabled.Checked == priorEnabled, "Tray enable restores");
        trayOptimize.PerformClick(); Check(trayOptimize.Checked == config.OptimizeMouseSpeed && config.OptimizeMouseSpeed != priorOptimize, "Tray optimize commits and checks");
        trayOptimize.PerformClick(); Check(config.OptimizeMouseSpeed == priorOptimize && trayOptimize.Checked == priorOptimize, "Tray optimize restores");
        var speedBefore = JsonSerializer.Serialize(config);
        main.EditDisplay(0);
        var speedEditor = main.DisplayEditor!;
        speedEditor.SelectTab(1);
        speedEditor.SpeedInput.Value = 125;
        string speedDevice = canvas.Monitors[0].DeviceName;
        Check(config.MouseSpeedFactor(speedDevice) == 1.25, "Preview affects runtime speed immediately");
        Check(JsonSerializer.Serialize(config) == speedBefore, "Preview never enters saved configuration");
        speedEditor.SpeedInput.ApplyWheel(120,false); Check(speedEditor.SpeedPercent == 130, "Wheel changes five percentage points");
        speedEditor.SpeedInput.ApplyWheel(-120,true); Check(speedEditor.SpeedPercent == 129, "Shift wheel changes one percentage point");
        speedEditor.SpeedInput.ApplyWheel(12000,false); Check(speedEditor.SpeedPercent == 300, "Wheel respects upper limit");
        speedEditor.SpeedInput.ApplyWheel(-12000,false); Check(speedEditor.SpeedPercent == 25, "Wheel respects lower limit");
        speedEditor.SpeedInput.Value = 125; Capture(speedEditor, "mouse-speed-preview");
        speedEditor.Back.PerformClick();
        Check(main.ScreensPage.Visible && main.DisplayEditor is null, "Back returns from embedded editor to screens");
        Check(config.MouseSpeedFactor(speedDevice) == 1 && JsonSerializer.Serialize(config) == speedBefore, "Window X rolls back preview exactly");
        main.EditDisplay(0); speedEditor = main.DisplayEditor!;
        speedEditor.SpeedInput.Value = 120;
        speedEditor.AcceptButton!.PerformClick();
        Check(main.DisplayEditor is null && config.MouseSpeedFactor(speedDevice) == 1.2, "Save closes modeless window and commits percent");
        Check(config.MouseSpeedPreview.Count == 0, "Save clears temporary preview");
        Check(config.Measurements.Count == 0, "Speed-only save does not adopt estimated physical dimensions");
        var speedReloaded = JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(config))!;
        Check(speedReloaded.MouseSpeedFactor(speedDevice) == 1.2, "Per-monitor correction roundtrips");
        main.EditDisplay(0); speedEditor = main.DisplayEditor!; speedEditor.SpeedInput.Value = 150;
        speedEditor.CancelButton!.PerformClick();
        Check(config.MouseSpeedFactor(speedDevice) == 1.2, "Cancel restores previously saved custom speed");
        config.MouseSpeedPercent.Remove(speedDevice);
        string mapsBefore = JsonSerializer.Serialize(config.Transitions);
        int calls = 0;
        main.DiagnosticGapEditor = (_, action, _) => { calls++; return calls == 1 ? 0 : null; };
        main.SelectGapMode(true);
        Check(!config.UsePhysicalGap && config.Gaps.Values.Single().EdgeDistanceMm == 0, "Cancellation retains earlier zero entry but does not enable mode");
        Check(trayGap.Checked == config.UsePhysicalGap, "Cancelled gap setup synchronized with tray");
        int missingCount = GapSetup.Missing(config.Transitions, config.Gaps).Count;
        calls = 0; main.DiagnosticGapEditor = (_, _, _) => { calls++; return 18; };
        main.SelectGapMode(true);
        Check(config.UsePhysicalGap && calls == missingCount, "Only missing connected pairs prompted");
        Check(trayGap.Checked == config.UsePhysicalGap, "Main gap control synchronizes tray");
        foreach (int width in new[] { 1100, 850 })
        {
            main.Size = new Size(width, width == 850 ? 500 : 700); Application.DoEvents(); Capture(main, "gap-main-" + width);
            foreach (string language in new[] { "EN", "LT", "RU" })
            {
                var languageWatch = System.Diagnostics.Stopwatch.StartNew();
                main.SelectLanguage(language); canvas.Update(); Application.DoEvents();
                languageTimings.Add(languageWatch.Elapsed.TotalMilliseconds);
                Check(canvas.PaintedLanguage == language, "Gap language repaints immediately without pointer movement");
                Capture(main, "layout-" + language + "-" + width);
                if (width == 1100)
                {
                    main.TrayMenu.Show(main, Point.Empty); Application.DoEvents();
                    using var menuImage = new Bitmap(main.TrayMenu.Width, main.TrayMenu.Height);
                    main.TrayMenu.DrawToBitmap(menuImage, new Rectangle(Point.Empty, menuImage.Size));
                    menuImage.Save(Path.Combine(Path.GetDirectoryName(output)!, "tray-" + language + ".png"));
                    foreach (ToolStripItem item in main.TrayMenu.Items)
                        Check(item.Width >= TextRenderer.MeasureText(item.Text, item.Font).Width + 24, "Tray translated text has check-column space");
                    main.TrayMenu.Close();
                }
                var brand = main.Controls.OfType<WindowHeader>().SelectMany(p => p.Controls.OfType<BrandHeader>()).Single();
                Check(brand.ClientRectangle.Contains(Rectangle.Ceiling(brand.MarkBounds)), "Brand icon contained");
                Check(brand.ClientRectangle.Contains(Rectangle.Ceiling(brand.WordBounds)), "Brand wordmark contained");
                Check(brand.MarkBounds.Right < brand.WordBounds.Left, "Brand icon separated from wordmark");
                Check(config.Language == language, "Language stored in config");
                Check(JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(config))!.Language == language, "Language roundtrip");
                Check(JsonSerializer.Serialize(config.Transitions) == mapsBefore, "Language preserves calibration");
                Check(main.LanguageControl.Parent!.ClientRectangle.Contains(main.LanguageControl.Bounds), "Language selector contained");
                var resource = main.ResourceLabel;
                Check(resource.Parent == main.AboutPage, "Telemetry belongs only to About page");

                foreach (var flow in main.Controls.OfType<FlowLayoutPanel>()) foreach (Control child in flow.Controls) Check(flow.ClientRectangle.Contains(child.Bounds), "Translated option contained");
                Check(main.OptionBandHeight == main.FooterBandHeight, "Option and footer bands have exactly equal heights");
                var screenArea = main.ScreensPage.Bounds;
                main.ToggleGuide(); Application.DoEvents(); Capture(main, "guide-" + language + "-" + width);
                Check(main.GuideControl.Bounds == screenArea && main.GuideControl.Bounds == main.ContentArea.ClientRectangle, "Guide fills complete monitor-and-prompt area");
                main.ToggleAbout(); Application.DoEvents(); Capture(main, "about-" + language + "-" + width);
                Check(TextRenderer.MeasureText(resource.Text, resource.Font).Width <= resource.Width - resource.Padding.Horizontal, "Visible About telemetry fits");
                Check(main.AboutPage.Bounds == screenArea && main.AboutPage.Bounds == main.ContentArea.ClientRectangle, "About matches complete content area");
                Check(!main.GuideControl.Visible && main.AboutPage.Visible && !main.ScreensPage.Visible, "Only requested page visible");
                pageGeometry.Add(new { language, window = main.Size, dpi = main.DeviceDpi, coordinateSpace = "workspace client pixels", screens = screenArea, guide = main.GuideControl.Bounds, about = main.AboutPage.Bounds });
                Check(!main.FooterActions.Bounds.IntersectsWith(main.ActivityControl.Bounds), "Footer buttons do not overlap activity status");
                main.AboutBack.PerformClick(); Application.DoEvents();
                Check(main.ScreensPage.Visible, "About back returns to screens");
                main.ToggleGuide(); main.GuideControl.Back.PerformClick();
                Check(main.ScreensPage.Visible, "Guide back returns to screens");
                main.EditDisplay(0); Application.DoEvents();
                var embedded = main.DisplayEditor!;
                Check(!embedded.TopLevel && embedded.Parent == main.ContentArea && embedded.Bounds == main.ContentArea.ClientRectangle, "Editor fills same content area without top-level window");
                Check(main.OptionBandHeight == main.FooterBandHeight, "Editor actions preserve equal band heights");
                Check(embedded.ActionBar.Parent != embedded && embedded.ActionBar.Parent!.ClientRectangle.Contains(embedded.ActionBar.Bounds), "Editor action bar lives inside main footer");
                Capture(main, "embedded-dimensions-" + language + "-" + width);
                embedded.SelectTab(1); Application.DoEvents(); Capture(main, "embedded-speed-" + language + "-" + width);
                Check(embedded.Controls.Cast<Control>().All(c => embedded.ClientRectangle.Contains(c.Bounds)), "Editor navigation, scroll viewport and actions remain contained");
                pageGeometry.Add(new { language, window = main.Size, dpi = main.DeviceDpi, editor = embedded.Bounds, topLevel = embedded.TopLevel, topBand = main.OptionBandHeight, footerBand = main.FooterBandHeight });
                embedded.SpeedInput.Value = 135; main.ToggleGuide();
                Check(main.DisplayEditor is null && config.MouseSpeedPreview.Count == 0 && main.GuideControl.Visible, "Navigating away cancels live preview");
                main.GuideControl.Back.PerformClick();
            }
            main.SelectLanguage("EN"); Application.DoEvents(); Capture(main, "gap-main-" + width);
            Check(canvas.GapBadges.Count == config.Transitions.Count, "One badge per connection");
            int primaryIndex = canvas.Monitors.ToList().FindIndex(m => m.Primary);
            var label = canvas.LabelMeasurements[primaryIndex];
            var textBounds = new Rectangle(label.Bounds.Left + (label.Bounds.Width - label.TextSize.Width) / 2, label.Bounds.Top + (label.Bounds.Height - label.TextSize.Height) / 2, label.TextSize.Width, label.TextSize.Height);
            Check(canvas.GapArrowBounds.All(r => !r.IntersectsWith(textBounds)), "Gap arrows do not overlap primary display title");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "gap-geometry-" + width + ".json"), JsonSerializer.Serialize(new { canvas.Size, canvas.DeviceDpi, primaryText = textBounds, arrows = canvas.GapArrowBounds, badges = canvas.GapBadges.Select(b => new { b.Badge, b.A, b.B }) }));
            foreach (var badge in canvas.GapBadges)
            {
                Check(canvas.ClientRectangle.Contains(badge.Badge), "Badge contained");
                int ia = canvas.Monitors.ToList().FindIndex(m => m.DeviceName == badge.Map.A), ib = canvas.Monitors.ToList().FindIndex(m => m.DeviceName == badge.Map.B);
                var a = canvas.LabelMeasurements[ia].Bounds; var b = canvas.LabelMeasurements[ib].Bounds;
                Check(canvas.CaseBounds[ia].Contains(a) && canvas.CaseBounds[ib].Contains(b), "Cases contain lit images");
                Check(badge.Map.Side switch { Edge.Left => badge.A.X == a.Left && badge.B.X == b.Right, Edge.Right => badge.A.X == a.Right && badge.B.X == b.Left, Edge.Top => badge.A.Y == a.Top && badge.B.Y == b.Bottom, _ => badge.A.Y == a.Bottom && badge.B.Y == b.Top }, "Gap witness anchors exactly on measured lit edges");
                Check(canvas.LabelMeasurements.All(m => !m.Bounds.IntersectsWith(badge.Badge)), "Gap badge outside every display");
                Check(canvas.GapAt(new Point(badge.Badge.Left + 4, badge.Badge.Top + 4)) == badge.Map, "Badge selects correct pair");
            }
        }
        main.SelectGapMode(false); Check(config.Gaps.Values.All(g => g.HasDistance), "Disabling keeps distances"); Capture(main, "gap-main-disabled");
        int edits = 0;
        main.DiagnosticGapEditor = (_, _, _) => { edits++; return 24; };
        var hit = canvas.GapBadges[0]; var screenPoint = canvas.PointToScreen(new Point(hit.Badge.Left + 4, hit.Badge.Top + 4));
        var data = new DataObject(DataFormats.Text, "BlakiBridge.GapTool");
        var drop = new DragEventArgs(data, 0, screenPoint.X, screenPoint.Y, DragDropEffects.Copy, DragDropEffects.None);
        typeof(Control).GetMethod("OnDragDrop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(canvas, [drop]); Application.DoEvents();
        Check(edits == 1 && config.Gaps[GapSettings.Key(hit.Map.A, hit.Map.B)].EdgeDistanceMm == 24, "Dropping tool edits selected pair");
        Check(!config.UsePhysicalGap, "Editing an off-mode gap does not enable compensation");
        Check(JsonSerializer.Serialize(config.Transitions) == mapsBefore, "Gap setup preserves calibration");
        var monitors = MonitorModel.Detect();
        foreach (string language in new[] { "EN", "LT", "RU" })
        {
            UiText.Language = language;
            using var gap = new PairGapForm(config.Transitions[0], monitors, 20); Offscreen(gap); ContainsControls(gap); Capture(gap, "localized-gap-" + language); gap.Close();
            using var display = new DisplaySettingsForm(monitors[0], 1, null); Offscreen(display); ContainsControls(display); Capture(display, "localized-display-" + language); display.SelectTab(1); display.SetSpeedEnabled(true); Application.DoEvents(); ContainsControls(display); Capture(display, "localized-speed-" + language); display.Close();
        }
        UiText.Language = "EN";
        foreach (var side in Enum.GetValues<Edge>())
        {
            var map = new Transition { A = monitors[0].DeviceName, B = monitors[1].DeviceName, Side = side };
            using var form = new PairGapForm(map, monitors, 18, "Next", "Set distance · 1 of 2"); Offscreen(form); ContainsControls(form); Capture(form, "gap-pair-" + side);
            Check(form.Diagram.Vertical == (side is Edge.Top or Edge.Bottom), "Pair orientation correct");
            foreach (var size in new[] { new Size(760, 470), new Size(724, 461) })
            {
                form.ClientSize = size; Application.DoEvents(); Capture(form, "gap-pair-" + side + "-" + size.Width);
                ContainsControls(form);
                Check(form.Entry.Top >= form.Diagram.Bottom, "Input below both monitors");
                Check(form.Diagram.ClientRectangle.Contains(form.Diagram.FirstImage) && form.Diagram.ClientRectangle.Contains(form.Diagram.SecondImage), "Lit images contained");
                Check(form.Diagram.Vertical ? form.Diagram.Start.Y == form.Diagram.FirstImage.Bottom && form.Diagram.End.Y == form.Diagram.SecondImage.Top : form.Diagram.Start.X == form.Diagram.FirstImage.Right && form.Diagram.End.X == form.Diagram.SecondImage.Left, "Dimension anchors on image edges");
            }
            form.Close();
        }
        using (var actual = new PairGapForm(config.Transitions.First(t => t.Side is Edge.Left or Edge.Right), monitors, 18))
        { Offscreen(actual); Capture(actual, "gap-pair-actual"); actual.Close(); }
        main.Close();
        using (var actualMain = new LiveMain(diagnosticConfig: BridgeConfig.Load()))
        { Offscreen(actualMain); Capture(actualMain, "current-layout-preview"); actualMain.Close(); }
        File.WriteAllText(output, JsonSerializer.Serialize(new { passed, pageGeometry, languageTimingsMs = languageTimings, note = "Offscreen UI; no user settings, hooks or diagnostics changed." }));
    }
    public static void ResourceUi(string output)
    {
        var results = new List<object>();
        using var main = new LiveMain(diagnosticConfig: new BridgeConfig());
        main.StartPosition = FormStartPosition.Manual;
        main.Location = new Point(SystemInformation.VirtualScreen.Right + 5000, SystemInformation.VirtualScreen.Bottom + 5000);
        main.ShowInTaskbar = false; main.Opacity = 0;
        main.Show(); Application.DoEvents();
        main.ToggleAbout(); Application.DoEvents();
        foreach (var size in new[] { new Size(1100, 700), new Size(850, 500) })
        {
            main.Size = size; main.PerformLayout();
            using var bitmap = new Bitmap(main.Width, main.Height);
            UiSnapshot.Paint(main,bitmap);
            var label = main.ResourceLabel;
            var textSize = TextRenderer.MeasureText(label.Text, label.Font);
            if (!label.Parent!.ClientRectangle.Contains(label.Bounds) || textSize.Width > label.Width - label.Padding.Horizontal || textSize.Height > label.Height - label.Padding.Vertical)
                throw new InvalidOperationException("Resource label clipped");
            if (!main.ResourceTimerRunning || main.Opacity != 0 || SystemInformation.VirtualScreen.IntersectsWith(main.Bounds)) throw new InvalidOperationException("Offscreen preview state invalid");
            string original=label.Text;label.Text="CPU: 100.00%   RAM: 99999.9 MB   Avg. background CPU: 100.00%";Application.DoEvents();
            var widest = AppText.MeasureText(label.Text,label.Font);
            if (widest.Width > label.Width - label.Padding.Horizontal) throw new InvalidOperationException("Resource values could clip");
            label.Text=original;
            bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"resources-{size.Width}.png"));
            results.Add(new { size, label.Bounds, label.Text, textSize, offscreen = true, sampling = main.ResourceTimerRunning });
        }
        main.Hide();
        if (main.ResourceTimerRunning) throw new InvalidOperationException("Resource timer remained active in tray");
        main.Show(); Application.DoEvents();
        if (!main.ResourceTimerRunning || main.ResourceLabel.Text.EndsWith("—")) throw new InvalidOperationException("Background average missing after reopening");
        main.Hide(); main.Close();
        File.WriteAllText(output, JsonSerializer.Serialize(new { passed = true, results }, new JsonSerializerOptions { WriteIndented = true }));
    }
    public static void StabilityUi(string output)
    {
        var config = JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(BridgeConfig.Load()))!;
        using var main = new LiveMain(diagnosticConfig: config);
        main.StartPosition = FormStartPosition.Manual;
        main.Location = new Point(SystemInformation.VirtualScreen.Right + 5000, SystemInformation.VirtualScreen.Bottom + 5000);
        main.ShowInTaskbar = false; main.Opacity = 0; main.Show(); Application.DoEvents();
        string before = JsonSerializer.Serialize(config);
        for (int i = 0; i < 100; i++)
        {
            main.Hide(); Application.DoEvents();
            if (main.ResourceTimerRunning) throw new InvalidOperationException("Background resource timer active at cycle " + i);
            main.Show(); Application.DoEvents();
            if (main.ResourceTimerRunning != main.AboutPage.Visible) throw new InvalidOperationException("Resource timer outside About at cycle " + i);
        }
        if (before != JsonSerializer.Serialize(config)) throw new InvalidOperationException("Visibility cycles altered configuration");
        var dispatch = typeof(LiveMain).GetMethod("WndProc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var checks = new List<string>();
        void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks.Add(name); }
        void Signal(int msg, int value) { var message = Message.Create(main.Handle, msg, (IntPtr)value, IntPtr.Zero); dispatch.Invoke(main, [message]); }
        var detected = MonitorModel.Detect();
        Check(main.PowerNotificationsRegistered, "Windows power notification registration succeeds");
        Check(detected.All(m => !string.IsNullOrWhiteSpace(m.DeviceIdentity)), "Current displays have verifiable identities");
        int starts = main.DiagnosticEngineStarts;
        Signal(0x218,4);
        Check(main.RecoveryPending && !main.RecoveryTimerRunning, "Suspend pauses without polling");
        main.ProcessRecoverySnapshot(detected); main.ProcessRecoverySnapshot(detected);
        Check(main.RecoveryPending && main.DiagnosticEngineStarts == starts, "No restart while suspended");
        Signal(0x218,18);
        main.ProcessRecoverySnapshot(detected);
        Check(main.RecoveryPending, "One sample cannot resume");
        main.ProcessRecoverySnapshot(detected);
        Check(!main.RecoveryPending && !main.RecoveryTimerRunning, "Stable wake recovers and stops polling");
        Check(main.DiagnosticEngineStarts == starts + (config.BridgeEnabled ? 1 : 0), "Wake respects enabled preference");
        Check(before == JsonSerializer.Serialize(config), "Wake preserves all settings and calibration");
        Signal(0x7E,0);
        main.ProcessRecoverySnapshot(detected.Take(1).ToArray());
        Check(main.RecoveryPending, "Disconnected display blocks recovery");
        main.ProcessRecoverySnapshot(detected); main.ProcessRecoverySnapshot(detected);
        Check(!main.RecoveryPending && before == JsonSerializer.Serialize(config), "Reconnection reuses exact maps");
        Signal(0x218,4); Signal(0x218,18);
        var enableItem = (ToolStripMenuItem)main.TrayMenu.Items[1];
        if (enableItem.Checked) enableItem.PerformClick();
        starts = main.DiagnosticEngineStarts;
        main.ProcessRecoverySnapshot(detected); main.ProcessRecoverySnapshot(detected);
        Check(!config.BridgeEnabled && !enableItem.Checked && main.DiagnosticEngineStarts == starts, "Explicit disable during recovery wins");
        // Use synthetic identities to test rejection without touching Windows or user displays.
        MonitorModel Display(string name, string identity, int x = 0, int width = 1920, uint dpi = 96) => new() { DeviceName = name, DeviceIdentity = identity, Bounds = new Rectangle(x,0,width,1080), WorkingArea = new Rectangle(x,0,width,1080), Primary = x == 0, WidthMm = 500, HeightMm = 280, DpiX = dpi, DpiY = dpi };
        var baseline = new[] { Display("A","hardware-A"), Display("B","hardware-B",1920) };
        var gate = new DisplayRecovery(); gate.SetBaseline(baseline);
        foreach (var incompatible in new[] {
            new[] { baseline[0] },
            new[] { baseline[0], Display("B","replacement-B",1920) },
            new[] { baseline[0], Display("B","hardware-B",1920,1280) },
            new[] { baseline[0], Display("B","hardware-B",2000) },
            new[] { baseline[0], Display("B","",1920) } })
        {
            gate.Begin(false); Check(!gate.Observe(incompatible) && !gate.Observe(incompatible) && gate.Pending, "Incompatible topology cannot restart");
        }
        gate.Begin(false);var renamed=new[]{baseline[0],Display("renamed","hardware-B",1920)};
        Check(!gate.Observe(renamed)&&gate.Observe(renamed)&&gate.Renames["B"]=="renamed"&&!gate.AcceptedDifferentSetup,"Unique hardware identity accepts renamed display after stable pair");
        gate.Begin(false);var ambiguous=new[]{Display("X","hardware-A"),Display("Y","hardware-A",1920)};
        Check(!gate.Observe(ambiguous)&&!gate.Observe(ambiguous),"Ambiguous identity never auto-recovers");
        var renameConfig=new BridgeConfig{BridgeEnabled=true,MouseSpeedPercent=new(){{"B",117}},DiagramWidths=new(){{"B",123}},MonitorPositions=new(){{"B",new Point(30,40)}},Connections=[new(){A="A",B="B",Side=Edge.Right}],Transitions=[new(){A="A",B="B",StartA=10,EndA=900,StartB=20,EndB=800,Side=Edge.Right,SizeA=new Size(1920,1080),SizeB=new Size(1920,1080)}],Gaps=new(){{GapSettings.Key("A","B"),new(){EdgeDistanceMm=23}}}};
        using var renameCanvas=new MonitorCanvas();renameCanvas.ManualWidths["B"]=123;renameCanvas.DiagramPositions["B"]=new PointF(10,20);
        MonitorRename.Apply(renameConfig,renameCanvas,new Dictionary<string,string>{{"B","renamed"}});
        Check(renameConfig.MouseSpeedPercent["renamed"]==117&&!renameConfig.MouseSpeedPercent.ContainsKey("B")&&renameConfig.Transitions[0].B=="renamed"&&renameConfig.Transitions[0].EndB==800&&renameConfig.Gaps[GapSettings.Key("A","renamed")].EdgeDistanceMm==23&&renameCanvas.DiagramPositions["renamed"]==new PointF(10,20)&&renameConfig.BridgeEnabled,"Rename retains calibration, speed, gap and enabled preference");
        var swapped=new Dictionary<string,int>{{"A",1},{"B",2}};MonitorRename.Keys(swapped,new Dictionary<string,string>{{"A","B"},{"B","A"}});Check(swapped["A"]==2&&swapped["B"]==1,"Simultaneous name swaps do not lose values");
        gate.Begin(false); Check(!gate.Observe(baseline), "Initial stable sample waits");
        Check(!gate.Observe([]) && !gate.Observe(baseline) && gate.Observe(baseline), "Interrupted stability requires two fresh samples");
        gate.Begin(false); for(int i=0;i<35;i++) gate.Observe([]);
        Check(gate.Pending && gate.ShouldPoll && gate.PollInterval==15000, "Missing screens switch to slow polling after30 attempts");
        gate.Begin(false); Check(!gate.Observe(baseline) && gate.Observe(baseline.Reverse().ToArray()), "New event restarts exhausted recovery; enumeration order irrelevant");
        var newDpi = new[] { Display("A","hardware-A",dpi:144), baseline[1] };
        gate.Begin(false); Check(!gate.Observe(newDpi) && gate.Observe(newDpi), "Same geometry with new DPI is safely refreshed");
        Check(JsonSerializer.Serialize(config.Transitions) == JsonSerializer.Serialize(JsonSerializer.Deserialize<BridgeConfig>(before)!.Transitions), "All recovery tests retain calibration");
        main.DiagnosticUseRecoveryTimer = true;
        Signal(0x218,4); Signal(0x218,18); main.Hide();
        Check(main.RecoveryTimerRunning, "Transient recovery timer runs while main is hidden");
        var recoveryWatch = System.Diagnostics.Stopwatch.StartNew();
        while (main.RecoveryPending && recoveryWatch.ElapsedMilliseconds < 10000) { Application.DoEvents(); System.Threading.Thread.Sleep(20); }
        Check(!main.RecoveryPending && !main.RecoveryTimerRunning, "Automatic timer path recovers from actual display snapshots and stops");
        Check(main.DiagnosticEngineStarts == starts && !config.BridgeEnabled, "Automatic path keeps explicit disable");
        main.Hide(); main.Close();
        File.WriteAllText(output, JsonSerializer.Serialize(new { passed = true, visibilityCycles = 100, checks, simulatedNotifications = new[] { "suspend", "resume-automatic", "display-change" }, configurationPreserved = true, limitations = "Diagnostic form with real identity detection and native power registration; no live hooks, physical sleep, hotplug or login exercised." }, new JsonSerializerOptions { WriteIndented = true }));
    }
    public static void Run(string output, bool visual = false)
    {
        var checks = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks.Add(name);
        }
        var redrawOrder = new List<string>();
        Check(CursorRedraw.Execute(() => redrawOrder.Add("hide"), () => { redrawOrder.Add("move"); return true; }, () => redrawOrder.Add("restore")), "Redraw move success preserved");
        Check(string.Join(",",redrawOrder)=="hide,move,restore", "Cursor erased before move and restored after");
        redrawOrder.Clear();
        Check(!CursorRedraw.Execute(() => redrawOrder.Add("hide"), () => false, () => redrawOrder.Add("restore")), "Redraw failure preserved");
        Check(redrawOrder.Last()=="restore", "Cursor restored after failed move");
        redrawOrder.Clear();
        try { CursorRedraw.Execute(() => redrawOrder.Add("hide"), () => throw new InvalidOperationException(), () => redrawOrder.Add("restore")); } catch (InvalidOperationException) { }
        Check(redrawOrder.Last()=="restore", "Cursor restored after exception");
        redrawOrder.Clear();
        try { CursorRedraw.Execute(() => throw new InvalidOperationException(), () => { redrawOrder.Add("move"); return true; }, () => redrawOrder.Add("restore")); } catch (InvalidOperationException) { }
        Check(string.Join(",",redrawOrder)=="restore", "Hide failure still invokes restoration without moving");
        Check(AbsoluteMouseInput.InputSize == (IntPtr.Size == 8 ? 40 : 28), "Native INPUT structure size");
        foreach (var axis in new[] { (origin: -2560, length: 7680), (origin: 0, length: 1920), (origin: -1600, length: 3520) })
        {
            for (int p=axis.origin;p<axis.origin+axis.length;p++)
            {
                int normalized=AbsoluteMouseInput.Normalize(p,axis.origin,axis.length);
                int reconstructed=axis.origin+(int)((long)normalized*axis.length/65536);
                Check(reconstructed==p, "Absolute coordinate pixel-center roundtrip");
            }
        }
        Check(AbsoluteMouseInput.Normalize(-999,0,100)==327, "Absolute input lower clamp");
        Check(AbsoluteMouseInput.Normalize(999,0,100)==65208, "Absolute input upper clamp");
        var deferred = new DeferredWarp();
        Action? posted = null;
        int applied = 0;
        Check(deferred.Queue(new Point(10,20), a => posted = a, p => { Check(p == new Point(10,20), "Deferred target unchanged"); applied++; }), "Crossing queued");
        Check(deferred.Pending && applied == 0, "No warp during queue/hook phase");
        Check(!deferred.Queue(Point.Empty, _ => { }, _ => applied++), "Only one pending crossing");
        posted!();
        Check(applied == 1 && !deferred.Pending, "Warp executes once after dispatch");
        posted(); Check(applied == 1, "Duplicate dispatch cannot warp again");
        deferred.Queue(Point.Empty, a => posted = a, _ => applied++);
        deferred.Cancel(); posted!();
        Check(applied == 1 && !deferred.Pending, "Disable cancels queued warp");
        try { deferred.Queue(Point.Empty, _ => throw new InvalidOperationException(), _ => { }); } catch (InvalidOperationException) { }
        Check(!deferred.Pending, "Scheduling failure clears pending");
        using (var dispatcher = new Control())
        {
            _ = dispatcher.Handle;
            deferred.Queue(Point.Empty, a => dispatcher.BeginInvoke(a), _ => applied++);
            Check(applied == 1, "Native dispatcher does not warp inline");
            Application.DoEvents();
            Check(applied == 2 && !deferred.Pending, "Native message pump applies deferred warp");
        }
        Check(CursorLanding.TryMeasureHandle(Cursors.Arrow.Handle, out var actualCursorSize, out var actualHotspot), "Native arrow bitmap measurement available");
        Check(actualCursorSize.Width > actualHotspot.X && actualCursorSize.Height > actualHotspot.Y, $"Runtime cursor {actualCursorSize} hotspot {actualHotspot}");
        foreach (var edge in Enum.GetValues<Edge>())
        {
            var landingTarget = new Rectangle(-400, -300, 400, 300);
            foreach (var cursorSize in new[] { new Size(32,32), new Size(96,96) })
            foreach (var hotspot in new[] { Point.Empty, new Point(cursorSize.Width/2, cursorSize.Height/2), new Point(cursorSize.Width-1,cursorSize.Height-1) })
            {
                var mapped = edge switch { Edge.Left => new Point(-2,-150), Edge.Right => new Point(-399,-150), Edge.Top => new Point(-200,-2), _ => new Point(-200,-299) };
                var landing = CursorLanding.Inset(landingTarget,edge,mapped,cursorSize,hotspot);
                var bitmap = new Rectangle(landing.X-hotspot.X,landing.Y-hotspot.Y,cursorSize.Width,cursorSize.Height);
                Check(landingTarget.Contains(bitmap), "Cursor bitmap contained after crossing " + edge);
                Check(Transition.Vertical(edge) ? landing.Y==mapped.Y : landing.X==mapped.X, "Calibrated along-edge position preserved " + edge);
            }
            var s = new Rectangle(0, 0, 400, 400);
            var t = edge switch { Edge.Left => new Rectangle(-400,0,400,400), Edge.Right => new Rectangle(400,0,400,400), Edge.Top => new Rectangle(0,-400,400,400), _ => new Rectangle(0,400,400,400) };
            Point At(Rectangle r, Edge e, int depth) => e switch { Edge.Left => new Point(r.Left+depth,200), Edge.Right => new Point(r.Right-1-depth,200), Edge.Top => new Point(200,r.Top+depth), _ => new Point(200,r.Bottom-1-depth) };
            var r = new MouseBridge.Route(s,t,edge,new Transition(),false);
            var traceWindow = new CrossingTraceWindow();
            Check(!traceWindow.Begin([r],At(s,edge,80),At(s,edge,60),0), "Trace ignores interior");
            Check(traceWindow.Begin([r],At(s,edge,51),At(s,edge,50),1), "Trace starts at 50px all directions");
            for (int i=0;i<1000;i++) traceWindow.Add(new { i });
            Check(traceWindow.Outcome(At(s,edge,1),2000) is null, "Trace survives slow approach and high event rate");
            Check(traceWindow.Outcome(At(t,Transition.Opposite(edge),49),2001) is null, "Trace includes target interior");
            Check(traceWindow.Outcome(At(t,Transition.Opposite(edge),50),2002) == "completed", "Trace ends at target 50px");
            Check(traceWindow.Outcome(At(s,edge,51),2002) == "retreated", "Retreat explicitly classified");
            Check(traceWindow.Outcome(At(s,edge,1),30002) == "safety_limit_incomplete", "Safety stop explicitly incomplete");
            traceWindow.Finish("completed",At(t,Transition.Opposite(edge),50));
            Check(!traceWindow.Active, "Session closes");
            Check(traceWindow.Begin([r],At(s,edge,51),At(s,edge,-10),30003), "Fast leap captures boundary event without cooldown");
        }
        Check(ResourceMeter.CpuPercent(1, 2, 16) == 3.125, "CPU percent normalized to total capacity");
        Check(ResourceMeter.CpuPercent(0, 5, 16) == 0, "Idle CPU delta reports zero");
        Check(ResourceMeter.CpuPercent(1, 0, 16) == 0, "CPU zero-duration sample safe");
        void CheckContents(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (!child.Visible) continue;
                Check(parent.ClientRectangle.Contains(child.Bounds), $"{parent.GetType().Name}: contains {child.GetType().Name} {child.Text}");
                CheckContents(child);
            }
        }
        foreach (var edge in Enum.GetValues<Edge>())
        {
            var t = new Transition { A = "first", B = "second", Side = edge, SizeA = new Size(5120, 1440), SizeB = new Size(2560, 1600), StartA = 100, EndA = 900, StartB = 20, EndB = 1520 };
            Check(t.Valid, $"{edge}: valid interval");
            Check(t.Map(100) == 20 && t.Map(900) == 1520 && t.Map(500) == 770, $"{edge}: endpoints and midpoint");
            for (int p = 100; p <= 900; p++) Check(Math.Abs(t.Map(t.Map(p), true) - p) < 0.000001, $"{edge}: roundtrip {p}");
            try { t.Map(99); throw new Exception("Outside interval accepted"); } catch (ArgumentOutOfRangeException) { }
            t.EndA = t.StartA;
            Check(!t.Valid, $"{edge}: zero interval rejected");
        }
        var config = new BridgeConfig { MonitorPositions = new() { ["negative"] = new Point(-2560, -400) }, Transitions = [new Transition { A = "a", B = "b", SizeA = new Size(100, 100), SizeB = new Size(200, 200), StartA = 0, EndA = 99, StartB = 0, EndB = 199 }] };
        var restored = JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(config))!;
        Check(restored.MonitorPositions["negative"] == new Point(-2560, -400), "negative layout coordinates survive JSON");
        Check(restored.Transitions.Single().Valid && restored.Transitions[0].Map(99) == 199, "transition survives JSON");
        var monitors = MonitorModel.Detect();
        Check(!new GapSettings().HasDistance && new GapSettings { EdgeDistanceMm = 0 }.HasDistance, "Missing and zero distance distinct");
        Check(!new GapSettings { EdgeDistanceMm = -1 }.HasDistance && !new GapSettings { EdgeDistanceMm = 2001 }.HasDistance, "Invalid gap distance rejected");
        var approximate = new BridgeConfig { OptimizeMouseSpeed = true };
        var currentMaps = LiveMain.BuildMaps(monitors, monitors.ToDictionary(m => m.DeviceName, m => m.Bounds.Location), []);
        using (var approximateEngine = new MouseBridge(currentMaps, monitors, approximate))
            Check(true, "Approximate speed starts without confirmed measurements");
        var sample = monitors[0];
        approximate.DiagramWidths[sample.DeviceName] = 250;
        var small = DisplaySizing.Resolve(sample, approximate.DiagramWidths, approximate.Measurements);
        approximate.DiagramWidths[sample.DeviceName] = 500;
        var large = DisplaySizing.Resolve(sample, approximate.DiagramWidths, approximate.Measurements);
        Check(Math.Abs(large.Width / small.Width - 2) < .001 && Math.Abs(large.Height / small.Height - 2) < .001, "Manual proportions preserve aspect ratio");
        Check(MouseBridge.SpeedFactor(sample.Bounds.Width, (decimal)small.Width, sample.Bounds.Width, (decimal)large.Width) == 2, "Halving manual size doubles pixel speed");
        var approximateReload = JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(approximate))!;
        Check(DisplaySizing.Resolve(sample, approximateReload.DiagramWidths, approximateReload.Measurements) == large, "Approximate speed proportions persist");
        Check(!new BridgeConfig().OptimizeMouseSpeed && !new GapSettings().Compensate, "Windows speed and instant gaps are defaults");
        Check(MouseBridge.SpeedFactor(2000, 500, 1000, 500) == 2, "Twice pixel density means twice pointer pixels");
        Check(MouseBridge.SpeedFactor(1000, 500, 1000, 500) == 1, "Primary speed unchanged");
        Check(Math.Abs(MouseBridge.DesktopSpeedFactor(2, 144, 96, true) - 4d/3) < 1e-10, "Remove Windows 150 percent scaling exactly once");
        Check(MouseBridge.DesktopSpeedFactor(2, 144, 96, false) == 2, "Linear Windows mode has no extra DPI correction");
        Check(MouseBridge.DesktopSpeedFactor(1, 144, 144, true) == 1, "Reference monitor is unchanged in precision mode");
        foreach(int input in new[]{1,4,16,64}) Check(Math.Abs(input * 1.5 * MouseBridge.DesktopSpeedFactor(2,144,96,true) - input * 2) < 1e-10, "Same density mapping at every input magnitude");
        double rx = 0, ry = 0; var cursor = Point.Empty;
        for (int i = 0; i < 100; i++) cursor = MouseBridge.ScaleMove(cursor, new Point(cursor.X + 1, cursor.Y - 1), .3, .7, ref rx, ref ry);
        Check(cursor == new Point(30, -70), "Fractional speed retains subpixel movement in both directions");
        config.OptimizeMouseSpeed = true;
        config.Measurements["a"] = new DisplayMeasurements { Width = 100, Height = 100, Right = 2 };
        config.Measurements["b"] = new DisplayMeasurements { Width = 200, Height = 200, Left = 3 };
        config.Gaps[GapSettings.Key("a", "b")] = new GapSettings { Millimeters = 5, Compensate = true };
        var physicalReload = JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(config))!;
        Check(physicalReload.OptimizeMouseSpeed && physicalReload.Measurements["a"].Right == 2 && physicalReload.Gaps[GapSettings.Key("b", "a")].Millimeters == 5, "Speed dimensions bezels and symmetric gap roundtrip");
        if (visual)
        {
        foreach (var monitor in monitors)
        {
            using var physical = new DisplaySettingsForm(monitor, 1, null);
            physical.Show(); Application.DoEvents();
            CheckContents(physical);
            foreach (var input in physical.Controls.OfType<TableLayoutPanel>().SelectMany(p => p.Controls.OfType<NumericUpDown>()))
                Check(input.Parent!.ClientRectangle.Contains(input.Bounds), $"Physical input fits {monitor.DeviceName} {input.Top}");
            using var bitmap = new Bitmap(physical.Width, physical.Height);
            physical.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"physical-{monitors.ToList().IndexOf(monitor)}.png"));
            IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
            string[] parameters = ["Active image width", "Active image height", "Left bezel", "Right bezel", "Top bezel", "Bottom bezel"];
            for (int parameter = 0; parameter < 6; parameter++)
            {
                var label = Descendants(physical).OfType<Label>().Single(l => l.Text == parameters[parameter]);
                typeof(Control).GetMethod("OnMouseEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(label, [EventArgs.Empty]);
                physical.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                Check(physical.Preview.Selected == parameter && physical.Preview.Caption.StartsWith(parameters[parameter]), "Hover highlights correct physical parameter");
                Check(physical.Preview.ClientRectangle.Contains(Rectangle.Ceiling(physical.Preview.CaseBounds)), "Monitor visualization contained");
                bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"physical-{monitors.ToList().IndexOf(monitor)}-parameter-{parameter}.png"));
            }
            physical.SetParameter(0, 600); physical.SetParameter(1, 300); physical.SetParameter(2, 12);
            physical.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            var renderedImage = physical.Preview.ImageBounds;
            Check(Math.Abs(renderedImage.Width / renderedImage.Height - 2) < .001 && physical.Preview.Caption.Contains("12 mm"), "Editing dimensions updates diagram and caption immediately");
            Descendants(physical).OfType<Button>().Single(b => b.Text == "Detected details").PerformClick(); Application.DoEvents(); CheckContents(physical);
            physical.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"physical-details-{monitors.ToList().IndexOf(monitor)}.png"));
            physical.Size = physical.MinimumSize; Application.DoEvents(); CheckContents(physical);
            using var minimumBitmap = new Bitmap(physical.Width, physical.Height);
            physical.DrawToBitmap(minimumBitmap, new Rectangle(Point.Empty, minimumBitmap.Size));
            minimumBitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"physical-minimum-{monitors.ToList().IndexOf(monitor)}.png"));
            physical.Close();
        }
        using (var gaps = new PairGapForm(currentMaps[0], monitors, null))
        {
            gaps.Show(); Application.DoEvents();
            CheckContents(gaps);
            using var bitmap = new Bitmap(gaps.Width, gaps.Height); gaps.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, "gaps.png")); gaps.Close();
        }
        var visualConfig = new BridgeConfig { AutoPhysicalSize = false, DiagramWidths = new() { ["test-a"] = 310 }, DiagramPositions = new() { ["test-a"] = new PointF(12, 34) } };
        var visualReloaded = JsonSerializer.Deserialize<BridgeConfig>(JsonSerializer.Serialize(visualConfig))!;
        Check(!visualReloaded.AutoPhysicalSize && visualReloaded.DiagramWidths["test-a"] == 310 && visualReloaded.DiagramPositions["test-a"].Y == 34, "Diagram sizing preferences roundtrip");
        foreach (bool auto in new[] { true, false })
        {
            using var main = new LiveMain(diagnosticConfig: new BridgeConfig { AutoPhysicalSize = auto });
            main.Show(); Application.DoEvents();
            foreach (var size in new[] { new Size(1100, 700), new Size(850, 500) })
            {
                main.Size = size; Application.DoEvents();
                using var bitmap = new Bitmap(main.Width, main.Height);
                UiSnapshot.Paint(main,bitmap);
                var monitorCanvas = main.CanvasControl;
                Check(monitorCanvas.LabelMeasurements.Count == monitors.Count, "Every display has a label");
                foreach (var label in monitorCanvas.LabelMeasurements)
                {
                    Check(label.Text.Contains(" × "), "Resolution is present in every display label");
                    Check(label.TextSize.Width <= label.Bounds.Width - 4 && label.TextSize.Height <= label.Bounds.Height - 4, $"Display label fits {auto} {size.Width}: {label.Text}");
                }
                bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"main-{auto}-{size.Width}.png"));
                foreach (var box in main.Controls.OfType<FlowLayoutPanel>().SelectMany(p => p.Controls.OfType<CheckBox>()).Where(c => c.Visible))
                    Check(box.Parent!.ClientRectangle.Contains(box.Bounds), $"Sizing toolbar fits {auto} {size.Width}: {box.Text}");
                string beforeGuide = JsonSerializer.Serialize(monitorCanvas.Positions);
                main.ToggleGuide(); Application.DoEvents();
                var guide = main.GuideControl;
                Check(guide.Visible && !monitorCanvas.Visible, "Help replaces diagram without a modal dialog");
                var guideContent = guide.Controls.OfType<FlowLayoutPanel>().Single();
                var guideLabels = guideContent.Controls.OfType<Label>().ToArray();
                Check(guideLabels.Length == 9, "Five steps and optional notes present");
                foreach (var label in guideLabels)
                    Check(label.Width <= guide.ClientSize.Width - guide.Padding.Horizontal && guideContent.ClientRectangle.Contains(label.Bounds), $"Guide text wraps within width {size.Width}");
                UiSnapshot.Paint(main,bitmap);
                bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"guide-{auto}-{size.Width}.png"));
                guide.AutoScrollPosition = new Point(0, guideContent.Height); Application.DoEvents();
                var last = guideLabels.Last();
                var lastBounds = new Rectangle(guide.PointToClient(last.PointToScreen(Point.Empty)), last.Size);
                Check(guide.ClientRectangle.Contains(lastBounds), "Emergency instruction reachable by scrolling");
                UiSnapshot.Paint(main,bitmap);
                bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"guide-end-{auto}-{size.Width}.png"));
                main.ToggleGuide(); Application.DoEvents();
                Check(monitorCanvas.Visible && !guide.Visible && beforeGuide == JsonSerializer.Serialize(monitorCanvas.Positions), "Back to screens preserves layout");
            }
            main.Close();
        }
        var resizeConfig = new BridgeConfig { OptimizeMouseSpeed = true };
        using (var main = new LiveMain(diagnosticConfig: resizeConfig))
        {
            main.Show(); Application.DoEvents();
            var canvas = main.CanvasControl;
            string mapsBefore = JsonSerializer.Serialize(resizeConfig.Transitions);
            var labels = canvas.LabelMeasurements;
            int index = monitors.ToList().FindIndex(m => m.Bounds.Width == 1920 && m.Bounds.Height == 480);
            if (index < 0) index = 0;
            var bounds = labels[index].Bounds;
            var oldSize = canvas.DisplaySize(monitors[index]);
            void Mouse(string method, int x, int y) => typeof(MonitorCanvas).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(canvas, [new MouseEventArgs(MouseButtons.Left, 1, x, y, 0)]);
            Mouse("OnMouseDown", bounds.Right - 8, bounds.Bottom - 8);
            Mouse("OnMouseMove", bounds.Right + 22, bounds.Bottom + 12);
            Mouse("OnMouseUp", bounds.Right + 22, bounds.Bottom + 12);
            Check(canvas.DisplaySize(monitors[index]).Width > oldSize.Width, "Dragging a corner resizes without an auto/manual switch");
            Check(resizeConfig.DiagramWidths.ContainsKey(monitors[index].DeviceName), "Corner drag persists speed proportions on release");
            Check(DisplaySizing.Resolve(monitors[index], resizeConfig.DiagramWidths, resizeConfig.Measurements) == canvas.DisplaySize(monitors[index]), "Engine and diagram use identical post-drag proportions");
            Check(mapsBefore == JsonSerializer.Serialize(resizeConfig.Transitions), "Resizing never changes calibrated transition endpoints");
            main.Close();
        }
        }
        string roundtripFile = Path.Combine(Path.GetDirectoryName(output)!, "test-settings.json");
        config.BridgeEnabled = true;
        config.Save(roundtripFile);
        var loadedFromDisk = BridgeConfig.Load(roundtripFile);
        Check(loadedFromDisk.BridgeEnabled && loadedFromDisk.Transitions[0].EndB == 199 && loadedFromDisk.MonitorPositions["negative"].X == -2560, "Actual settings file reload preserves state and calibration");
        loadedFromDisk.Save(roundtripFile);
        Check(File.Exists(roundtripFile + ".bak"), "Previous settings backed up on replace");
        File.WriteAllText(roundtripFile, "broken diagnostic fixture");
        Check(BridgeConfig.Load(roundtripFile).Transitions.Count == 1, "Corrupt settings recover from backup");
        foreach (var side in Enum.GetValues<Edge>())
        {
            var mapping = new Transition { A = "test-a", B = "test-b", Side = side, SizeA = new Size(100, 100), SizeB = new Size(200, 200), StartA = 10, EndA = 90, StartB = 20, EndB = 180 };
            var route = new MouseBridge.Route(new Rectangle(-100, 0, 100, 100), new Rectangle(100, 100, 200, 200), side, mapping, false);
            var from = new Point(-50, 50);
            var to = side switch { Edge.Left => new Point(-110, 50), Edge.Right => new Point(10, 50), Edge.Top => new Point(-50, -10), _ => new Point(-50, 110) };
            var expected = side switch { Edge.Left => new Point(298, 200), Edge.Right => new Point(101, 200), Edge.Top => new Point(200, 298), _ => new Point(200, 101) };
            Check(MouseBridge.MapMove(route, from, to) == expected, $"{side}: global crossing destination");
            Check(MouseBridge.MapMove(route, from, new Point(-49, 50)) is null, $"{side}: interior movement unchanged");
            MonitorModel Fake(string id, Rectangle bounds, bool primary) => new() { DeviceName = id, Bounds = bounds, WorkingArea = bounds, Primary = primary, WidthMm = bounds.Width, HeightMm = bounds.Height, DpiX = 96, DpiY = 96 };
            var physicalConfig = new BridgeConfig
            {
                UsePhysicalGap = true,
                Measurements = new()
                {
                    ["test-a"] = new() { Width = 100, Height = 100, Left = 2, Right = 2, Top = 2, Bottom = 2 },
                    ["test-b"] = new() { Width = 200, Height = 200, Left = 3, Right = 3, Top = 3, Bottom = 3 }
                },
                Gaps = new() { [GapSettings.Key("test-a", "test-b")] = new() { EdgeDistanceMm = 10 } }
            };
            using var engine = new MouseBridge([mapping], [Fake("test-a", route.Source, true), Fake("test-b", route.Target, false)], physicalConfig);
            var edgePoint = side switch { Edge.Left => new Point(-100, 50), Edge.Right => new Point(-1, 50), Edge.Top => new Point(-50, 0), _ => new Point(-50, 99) };
            for (int step = 0; step < 9; step++) Check(route.Source.Contains(engine.Compensate(route, expected, edgePoint)), $"{side}: gap holds movement {step}");
            Check(engine.Compensate(route, expected, edgePoint) == expected, $"{side}: edge-to-edge gap does not add bezels twice");
            physicalConfig.UsePhysicalGap = false;
            Check(engine.Compensate(route, expected, edgePoint) == expected, $"{side}: default instant crossing ignores gap");
        }
        if (visual) using (var dialog = new Form { Text = "BlakiBridge alignment verification" })
        {
            dialog.Show();
            var positions = monitors.ToDictionary(m => m.DeviceName, m => m.Bounds.Location);
            var liveMaps = LiveMain.BuildMaps(monitors, positions, []);
            Check(liveMaps.Count > 0 && liveMaps.All(m => m.Valid), "Live starting layout produces valid maps");
            Check(LiveMain.BuildMaps(monitors, positions, liveMaps).SequenceEqual(liveMaps), "Existing calibration preserved");
            using (var live = new LiveMarkers(dialog, liveMaps, monitors, _ => { }, () => { }))
            {
                Application.DoEvents();
                var handles = Application.OpenForms.OfType<MarkerHandle>().ToArray();
                Check(handles.Length == liveMaps.Count * 4, "All live endpoint handles visible together");
                foreach (var h in handles)
                {
                    Check(h.Visible && monitors.Any(m => m.Bounds.Contains(h.Bounds)), $"Live handle stays inside monitor: {h.Bounds}, visible={h.Visible}");
                    using var bitmap = new Bitmap(h.Width, h.Height);
                    h.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, $"live-marker-{h.Left}-{h.Top}.png"));
                }
            }
            dialog.Close();
        }
        if (!visual) Check(Application.OpenForms.Count == 0, "Silent self-test creates no application windows");
        File.WriteAllText(output, JsonSerializer.Serialize(new { passed = checks.Count, mode = visual ? "visual" : "silent", checks, monitors, note = "System physical sizes are unverified; no user configuration changed." }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
