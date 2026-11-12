namespace BlakiBridge;
// The native list keeps keyboard navigation and accessibility; only its closed chrome is replaced.
internal sealed class ChoiceField : UserControl
{
    private readonly ComboBox choice;
    private readonly Panel clip=new(){BackColor=AppTheme.Background};
    private readonly DropArrow arrow;
    internal ChoiceField(ComboBox input)
    {
        choice=input;AutoScaleMode=AutoScaleMode.None;Height=32;Dock=DockStyle.Top;BackColor=AppTheme.Background;TabStop=false;
        DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);
        choice.Dock=DockStyle.None;choice.FlatStyle=FlatStyle.Flat;clip.Controls.Add(choice);arrow=new DropArrow(this);Controls.AddRange([clip,arrow]);
        choice.GotFocus+=(_,_)=>Invalidate();choice.LostFocus+=(_,_)=>Invalidate();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);if(arrow is null)return;float s=DeviceDpi/96f;int h=(int)(32*s),aw=(int)(30*s);
        if(Height!=h)Height=h;
        clip.SetBounds((int)(7*s),(Height-choice.PreferredHeight+4)/2,Math.Max(1,Width-aw-(int)(10*s)),Math.Max(1,choice.PreferredHeight-4));
        choice.SetBounds(-2,-2,clip.Width+aw+4,choice.PreferredHeight);arrow.SetBounds(Width-aw,(int)(6*s),aw-(int)(6*s),Height-(int)(12*s));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        Chrome.Draw(this,e.Graphics,AppTheme.Background,AppTheme.Background,ContainsFocus?AppTheme.Accent:AppTheme.Border);
    }
    private sealed class DropArrow(ChoiceField owner):Control
    {
        protected override void OnMouseDown(MouseEventArgs e){owner.choice.Focus();owner.choice.DroppedDown=!owner.choice.DroppedDown;}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Background);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;float s=DeviceDpi/96f,x=Width/2f,y=Height/2f;using var pen=new Pen(AppTheme.Text,1.4f*s);e.Graphics.DrawLines(pen,[new PointF(x-4*s,y-2*s),new PointF(x,y+2*s),new PointF(x+4*s,y-2*s)]);}
    }
}
