namespace BlakiBridge;
internal sealed class StatusDot : Control
{
    internal StatusDot(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Background);
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float d=Math.Min(Width,Height)*.72f;using var brush=new SolidBrush(ForeColor);
        e.Graphics.FillEllipse(brush,(Width-d)/2,(Height-d)/2,d,d);
    }
}
