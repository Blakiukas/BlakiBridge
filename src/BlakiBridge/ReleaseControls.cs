using System.Drawing.Drawing2D;
namespace BlakiBridge;
internal sealed class AdjustButton : CheckBox
{
    private bool hover;
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    public AdjustButton(){Cursor=Cursors.Hand;AutoSize=true;Appearance=Appearance.Button;FlatStyle=FlatStyle.Flat;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);CheckedChanged+=(_,_)=>RefreshText();}
    private string Caption(bool active)=>active?UiText.Pick("Finish adjusting","Baigti reguliavimą","Завершить настройку"):UiText.T("Adjust");
    internal void RefreshText(){Text=Caption(Checked);Invalidate();}
    public override Size GetPreferredSize(Size size)=>new(Math.Max(AppText.MeasureText(Caption(false),Font).Width,AppText.MeasureText(Caption(true),Font).Width)+(int)(24*DeviceDpi/96f),(int)(32*DeviceDpi/96f));
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent?.BackColor??AppTheme.Background);Chrome.Draw(this,g,Checked?AppTheme.Accent:hover?Color.FromArgb(66,66,66):AppTheme.Surface,Checked?AppTheme.Accent:AppTheme.Background,Checked?AppTheme.Accent:hover?Color.FromArgb(190,190,190):AppTheme.Border);AppText.DrawText(g,Text,Font,ClientRectangle,!Enabled?Color.Gray:Checked?Color.Black:AppTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-5,-5));}
}
internal sealed class DetailIconButton : RoundedButton
{
    internal bool RefreshIcon,EmailIcon;
    public DetailIconButton(){AutoSize=false;Size=new Size(32,32);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(Parent?.BackColor ?? AppTheme.Background);var state=g.Save();float s=DeviceDpi/96f;
        g.TranslateTransform(Width/2f,Height/2f);g.ScaleTransform(s,s);g.SmoothingMode=SmoothingMode.AntiAlias;
        if(Hover&&Enabled){using var glow=new SolidBrush(Color.FromArgb(40,255,255,255));g.FillEllipse(glow,-14,-14,28,28);}
        var color=EmailIcon||!RefreshIcon?AppTheme.Accent:Color.FromArgb(180,180,180);
        using var pen=new Pen(color,1.6f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        if(EmailIcon){g.DrawRectangle(pen,-8,-5.5f,16,11);g.DrawLines(pen,[new PointF(-8,-5),new PointF(0,1),new PointF(8,-5)]);}
        else if(RefreshIcon){g.DrawArc(pen,-6,-6,12,12,210,150);g.DrawLines(pen,[new PointF(3,-2),new PointF(6,0),new PointF(8,-3)]);g.DrawArc(pen,-6,-6,12,12,30,150);g.DrawLines(pen,[new PointF(-3,2),new PointF(-6,0),new PointF(-8,3)]);}
        else{using var ring=new Pen(Color.WhiteSmoke,0.8f);g.DrawEllipse(ring,-14,-14,28,28);using var ink=new SolidBrush(color);using var mark=new GraphicsPath();mark.AddBezier(-3,-9,-5,-14,5,-14,3,-7);mark.AddBezier(3,-7,2,-3,0,1,-1,2);mark.AddBezier(-1,2,-3,3,-3,0,-3,-9);mark.CloseFigure();g.FillPath(ink,mark);g.FillEllipse(ink,-3,5,5,5);}
        g.Restore(state);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-5,-5));
    }
}

internal sealed class ResourceExplanationLabel : Label
{
    internal bool HighlightResources;
    public ResourceExplanationLabel(){UseCompatibleTextRendering=true;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
    protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent?.BackColor??AppTheme.Background);Chrome.Draw(this,e.Graphics,AppTheme.Surface,AppTheme.Surface,AppTheme.Border);}
    protected override void OnPaint(PaintEventArgs e){if(HighlightResources){ResourceText.Draw(e.Graphics,Text,Font,ClientRectangle,Padding,ForeColor,false);return;}e.Graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;base.OnPaint(e);}
}
