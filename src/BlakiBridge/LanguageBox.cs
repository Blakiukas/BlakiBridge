using System.Drawing.Drawing2D;
namespace BlakiBridge;

// A real button and accessible menu own all their painting; no native combo overlay.
internal sealed class LanguageBox : Button
{
    private readonly ContextMenuStrip menu = new() { ShowImageMargin = false, ShowCheckMargin = true };
    private string selected = "EN";
    private bool hover;
    public event EventHandler? SelectedIndexChanged;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string SelectedItem
    {
        get => selected;
        set
        {
            if (value is not ("EN" or "LT" or "RU") || value == selected) return;
            selected = value; Text = value; AccessibleDescription = "Language: " + value;
            foreach (ToolStripMenuItem item in menu.Items) item.Checked = item.Text == value;
            Invalidate(); AccessibilityNotifyClients(AccessibleEvents.NameChange, -1);
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public LanguageBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(96, 32); Text = selected; AccessibleName = "Language";
        AccessibleDescription = "Language: EN";
        BackColor = AppTheme.Background; ForeColor = Color.WhiteSmoke;
        menu.Renderer = new DarkRenderer();
        menu.BackColor = BackColor; menu.ForeColor = ForeColor;
        foreach (var code in new[] { "EN", "LT", "RU" })
        {
            var item = new ToolStripMenuItem(code) { Checked = code == selected, AccessibleName = code };
            item.Click += (_, _) => SelectedItem = code;
            menu.Items.Add(item);
        }
        menu.Closed += (_, _) => Invalidate();
    }
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (menu.Visible) { menu.Close(); return; }
        menu.Font = Font; menu.Show(this, new Point(0, Height));
        foreach (ToolStripMenuItem item in menu.Items) if (item.Checked) item.Select();
        Invalidate();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is Keys.Down or Keys.Up or Keys.F4 || keyData == (Keys.Alt | Keys.Down))
        { PerformClick(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; float s = DeviceDpi / 96f;
        Color fill=hover||menu.Visible?AppTheme.Surface:AppTheme.Background;
        Chrome.Draw(this,g,fill,fill,Focused&&ShowFocusCues?AppTheme.Accent:AppTheme.Border);
        g.SmoothingMode=SmoothingMode.AntiAlias;
        float cy = Height/2f;
        using var ink = new Pen(ForeColor, 1.2f*s);
        g.DrawEllipse(ink, 9*s, cy-7*s, 14*s,14*s); g.DrawEllipse(ink,13*s,cy-7*s,6*s,14*s); g.DrawLine(ink,9*s,cy,23*s,cy);
        AppText.DrawText(g, selected, Font, new Rectangle((int)(28*s),0,Width-(int)(50*s),Height), ForeColor, TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
        g.DrawLines(ink, [new PointF(Width-16*s,cy-2*s),new PointF(Width-12*s,cy+2*s),new PointF(Width-8*s,cy-2*s)]);
    }
    protected override void Dispose(bool disposing) { if (disposing) menu.Dispose(); base.Dispose(disposing); }
    private sealed class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColors()) { RoundedEdges = false; }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            using var fill = new SolidBrush(e.Item.Selected ? Color.FromArgb(54,54,54) : AppTheme.Background);
            e.Graphics.FillRectangle(fill, new Rectangle(Point.Empty, e.Item.Size));
        }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            using var fill = new SolidBrush(Color.FromArgb(54,54,54));
            e.Graphics.FillRectangle(fill, e.ImageRectangle);
            using var pen = new Pen(Color.WhiteSmoke, 1.5f);
            var r = e.ImageRectangle;
            e.Graphics.DrawLines(pen, [new PointF(r.Left+r.Width*.2f,r.Top+r.Height*.5f),new PointF(r.Left+r.Width*.43f,r.Top+r.Height*.73f),new PointF(r.Left+r.Width*.8f,r.Top+r.Height*.25f)]);
        }
    }
    private sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => AppTheme.Background;
        public override Color ImageMarginGradientBegin => ToolStripDropDownBackground;
        public override Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
        public override Color ImageMarginGradientEnd => ToolStripDropDownBackground;
        public override Color MenuItemSelected => Color.FromArgb(54,54,54);
        public override Color MenuItemBorder => Color.FromArgb(97,97,97);
        public override Color MenuBorder => MenuItemBorder;
        public override Color CheckBackground => MenuItemSelected;
        public override Color CheckSelectedBackground => MenuItemSelected;
        public override Color CheckPressedBackground => MenuItemSelected;
    }
}
