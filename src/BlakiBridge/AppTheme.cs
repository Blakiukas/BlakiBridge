using System.Drawing.Drawing2D;
namespace BlakiBridge;

internal static class AppTheme
{
    internal static readonly Color Background = Color.FromArgb(24,24,24);
    internal static readonly Color Surface = Color.FromArgb(32,32,32);
    internal static readonly Color Border = Color.FromArgb(90,90,90);
    internal static readonly Color Text = Color.FromArgb(242,242,242);
    internal static readonly Color Accent = Color.FromArgb(255,151,0);
    internal static readonly Color Measurement = Color.FromArgb(210,210,210);
    internal static void Dimension(Graphics g,PointF a,PointF b,float scale)
    {
        float dx=b.X-a.X,dy=b.Y-a.Y,length=MathF.Sqrt(dx*dx+dy*dy);if(length<1)return;
        dx/=length;dy/=length;float head=Math.Min(9*scale,length/3),half=4*scale;
        using var pen=new Pen(Measurement,Math.Max(2,2*scale));using var fill=new SolidBrush(Measurement);
        g.DrawLine(pen,a,b);
        foreach(var item in new[]{(a,1f),(b,-1f)})
        {var tip=item.Item1;float direction=item.Item2;var back=new PointF(tip.X+dx*head*direction,tip.Y+dy*head*direction);g.FillPolygon(fill,[tip,new PointF(back.X-dy*half,back.Y+dx*half),new PointF(back.X+dy*half,back.Y-dx*half)]);}
    }
    internal static void ScreenFill(Graphics graphics,RectangleF bounds,bool primary=false,Control? owner=null)
    {
        if(bounds.Width<=0 || bounds.Height<=0)return;
        using var fill=new LinearGradientBrush(bounds,Color.FromArgb(66,66,66),Color.FromArgb(40,40,40),90f);
        graphics.FillRectangle(fill,bounds);
    }
    internal static GraphicsPath OpenTop(RectangleF r,float radius)
    {
        var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
        p.AddLine(r.Left,r.Top,r.Left,r.Bottom-radius);p.AddArc(r.Left,r.Bottom-d,d,d,180,-90);
        p.AddLine(r.Left+radius,r.Bottom,r.Right-radius,r.Bottom);p.AddArc(r.Right-d,r.Bottom-d,d,d,90,-90);
        p.AddLine(r.Right,r.Bottom-radius,r.Right,r.Top);return p;
    }
    internal static GraphicsPath Round(RectangleF r,float radius)
    {
        var p=new GraphicsPath(); float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
        if(d<=0){p.AddRectangle(r);return p;}
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
}
internal class RoundedButton : Button
{
    protected bool Hover, Pressed;
    internal bool IsSelected;
    public RoundedButton()
    {
        Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat; FlatAppearance.BorderSize=0;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        BackColor=AppTheme.Surface;ForeColor=AppTheme.Text;
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text=AppText.MeasureText(Text,Font);int icon=Image is null?0:(int)((Image.Width+6)*DeviceDpi/96f);
        return new Size(Math.Max(MinimumSize.Width,text.Width+icon+Math.Max(Padding.Horizontal,(int)(24*DeviceDpi/96f))),Math.Max(MinimumSize.Height,Math.Max(text.Height+Math.Max(Padding.Vertical,(int)(10*DeviceDpi/96f)),(int)(32*DeviceDpi/96f))));
    }
    protected override void OnMouseEnter(EventArgs e){Hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){Hover=false;Pressed=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){Pressed=true;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){Pressed=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(Parent?.BackColor??AppTheme.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
        float s=DeviceDpi/96f;
        bool accent=BackColor.ToArgb()==Color.DarkOrange.ToArgb();
        Color top=IsSelected?Color.FromArgb(52,52,52):accent?Color.FromArgb(255,167,0):Hover&&Enabled?Color.FromArgb(66,66,66):Color.FromArgb(31,31,31);
        Color bottom=accent?Color.FromArgb(255,143,0):AppTheme.Background;
        if(Pressed)top=bottom;
        Chrome.Draw(this,g,top,bottom,accent||IsSelected?AppTheme.Accent:Focused&&ShowFocusCues?AppTheme.Accent:Hover&&Enabled?Color.FromArgb(190,190,190):AppTheme.Border);
        var text=Rectangle.Inflate(ClientRectangle,-(int)(10*s),-2);
        if(Image is not null)
        {
            int width=(int)(Image.Width*s),height=(int)(Image.Height*s),x=text.Left,y=(Height-height)/2;
            if(Tag as string=="book")
            {
                var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);using var ink=new Pen(AppTheme.Text,1.4f){LineJoin=LineJoin.Round};
                g.DrawLines(ink,[new Point(11,5),new Point(6,3),new Point(2,4),new Point(2,17),new Point(6,16),new Point(11,18),new Point(16,16),new Point(20,17),new Point(20,4),new Point(16,3),new Point(11,5),new Point(11,18)]);g.Restore(state);
            }
            else if(Tag as string=="coffee")
            {
                var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);using var ink=new Pen(AppTheme.Accent,1.6f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round};
                g.DrawLines(ink,[new Point(3,7),new Point(3,13),new Point(5,16),new Point(12,16),new Point(14,13),new Point(14,7),new Point(3,7)]);g.DrawArc(ink,12,8,7,6,-90,180);g.DrawLine(ink,2,19,17,19);g.DrawLine(ink,6,2,6,4);g.DrawLine(ink,10,1,10,4);g.Restore(state);
            }
            else g.DrawImage(Image,new Rectangle(x,y,width,height));
            text.X+=width+(int)(6*s);text.Width-=width+(int)(6*s);
        }
        AppText.DrawText(g,Text,Font,text,!Enabled?Color.Gray:accent?Color.Black:AppTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
    }
}
internal sealed class PageTabButton : RoundedButton
{
    internal bool Selected;
    public override Size GetPreferredSize(Size proposedSize)=>new(Math.Max((int)(155*DeviceDpi/96f),AppText.MeasureText(Text,Font).Width+(int)(44*DeviceDpi/96f)),(int)(44*DeviceDpi/96f));
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(Parent?.BackColor??AppTheme.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
        Chrome.Draw(this,g,Selected?Color.FromArgb(52,52,52):AppTheme.Background,AppTheme.Surface,Selected?AppTheme.Accent:Hover&&Enabled?Color.FromArgb(190,190,190):AppTheme.Border,openBottom:true,thickness:Selected?2:1);
        AppText.DrawText(g,Text,Font,ClientRectangle,Enabled?AppTheme.Text:Color.FromArgb(119,119,119),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-8,-8));
    }
}
internal class RoundedNumber : UserControl
{
    public override Size GetPreferredSize(Size proposedSize)=>new((int)(140*DeviceDpi/96f),(int)(32*DeviceDpi/96f));
    private readonly NumberEditor editor;
    private readonly Panel clip;
    private readonly ArrowSurface arrows;
    private readonly NumberReadout readout;
    private bool arranging;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal decimal Minimum {get=>editor.Minimum;set=>editor.Minimum=value;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal decimal Maximum {get=>editor.Maximum;set=>editor.Maximum=value;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal decimal Value {get=>editor.Value;set=>editor.Value=value;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal decimal Increment {get=>editor.Increment;set=>editor.Increment=value;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal int DecimalPlaces {get=>editor.DecimalPlaces;set=>editor.DecimalPlaces=value;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal HorizontalAlignment TextAlign {get=>editor.TextAlign;set=>editor.TextAlign=value;}
    internal event EventHandler? ValueChanged;
    public RoundedNumber()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        AutoScaleMode=AutoScaleMode.None;
        Height=32;Width=140;TabStop=false;BackColor=AppTheme.Background;ForeColor=AppTheme.Text;
        clip=new Panel {BackColor=AppTheme.Background};
        editor=new NumberEditor(this) {BorderStyle=BorderStyle.None,BackColor=AppTheme.Background,ForeColor=AppTheme.Text};
        clip.Controls.Add(editor);arrows=new ArrowSurface(this);readout=new NumberReadout(this);clip.Controls.Add(readout);readout.BringToFront();Controls.AddRange([clip,arrows]);
        editor.ValueChanged+=(_,e)=>{readout.Invalidate();ValueChanged?.Invoke(this,e);};
        editor.TextChanged+=(_,_)=>readout.Invalidate();
        editor.Enter+=(_,_)=>{readout.Editing(true);Invalidate();};editor.Leave+=(_,_)=>{readout.Editing(false);Invalidate();};
        Resize+=(_,_)=>Arrange();FontChanged+=(_,_)=>Arrange();editor.FontChanged+=(_,_)=>Arrange();editor.SizeChanged+=(_,_)=>Arrange();Arrange();
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Height=(int)(32*DeviceDpi/96f);Arrange();}
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);Height=(int)(32*DeviceDpi/96f);Arrange();}
    private void Arrange()
    {
        if(arranging)return;arranging=true;SuspendLayout();clip.SuspendLayout();try {
        float s=DeviceDpi/96f;int aw=(int)(30*s);
        clip.SetBounds((int)(9*s),(int)Math.Ceiling(2*s),Math.Max(10,Width-aw-(int)(15*s)),Height-2*(int)Math.Ceiling(2*s));
        editor.SetBounds(0,(clip.Height-editor.PreferredHeight)/2,clip.Width+SystemInformation.VerticalScrollBarWidth+8,editor.PreferredHeight);
        readout.Bounds=clip.ClientRectangle;
        arrows.SetBounds(Math.Max(0,Width-aw),(int)(6*s),aw-(int)(6*s),Height-(int)(12*s));
        } finally { clip.ResumeLayout(false);ResumeLayout(false);arranging=false; }
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if(e is HandledMouseEventArgs handled)handled.Handled=true;
        if(!Enabled)return;Value=Math.Clamp(Value+e.Delta/120*Increment,Minimum,Maximum);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        Chrome.Draw(this,e.Graphics,AppTheme.Background,AppTheme.Background,ContainsFocus?AppTheme.Accent:AppTheme.Border);
    }
    private sealed class NumberReadout : Control
    {
        private readonly RoundedNumber owner;
        private readonly System.Windows.Forms.Timer blink=new(){Interval=500};
        private bool caret=true;
        private int anchor;
        private float scroll;
        private TextBox? Input=>owner.editor.Controls.OfType<TextBox>().FirstOrDefault();
        internal NumberReadout(RoundedNumber parent)
        {
            owner=parent;TabStop=false;AccessibleRole=AccessibleRole.None;Cursor=Cursors.IBeam;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
            blink.Tick+=(_,_)=>{if(!owner.editor.ContainsFocus){blink.Stop();return;}caret=!caret;Invalidate();};
            if(Input is { } input){input.GotFocus+=(_,_)=>Editing(true);input.LostFocus+=(_,_)=>Editing(false);}
            owner.editor.KeyUp+=(_,_)=>{caret=true;Invalidate();};
        }
        internal void Editing(bool active){caret=true;if(active)blink.Start();else {blink.Stop();scroll=0;}Invalidate();}
        private static StringFormat Format()=>new(StringFormat.GenericTypographic){FormatFlags=StringFormatFlags.NoWrap|StringFormatFlags.MeasureTrailingSpaces};
        private float Advance(Graphics g,int count)
        {
            if(count<=0)return 0;using var f=Format();return g.MeasureString(owner.editor.Text[..Math.Min(count,owner.editor.Text.Length)],owner.editor.Font,int.MaxValue,f).Width;
        }
        private float Origin(Graphics g)=>owner.editor.TextAlign switch {HorizontalAlignment.Center=>Math.Max(0,(Width-Advance(g,owner.editor.Text.Length))/2),HorizontalAlignment.Right=>Math.Max(0,Width-Advance(g,owner.editor.Text.Length)),_=>0};
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.Clear(Parent?.BackColor ?? AppTheme.Background);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var font=owner.editor.Font;float y=AppText.LineTop(g,font,Height);
            var input=Input;int start=Math.Min(input?.SelectionStart??0,owner.editor.Text.Length),length=Math.Min(input?.SelectionLength??0,owner.editor.Text.Length-start);
            float origin=Origin(g),left=origin+Advance(g,start),right=origin+Advance(g,start+length);
            if(owner.editor.ContainsFocus){float end=length==0?left:right;if(end-scroll>Width-2)scroll=end-Width+2;if(left-scroll<0)scroll=left;}
            using var format=Format();using var ink=new SolidBrush(owner.Enabled?AppTheme.Text:Color.Gray);
            if(owner.editor.ContainsFocus && length>0){using var selection=new SolidBrush(Color.FromArgb(65,255,255,255));g.FillRectangle(selection,left-scroll,y,Math.Max(1,right-left),font.GetHeight(g));}
            g.DrawString(owner.editor.Text,font,ink,new PointF(origin-scroll,y),format);
            if(owner.editor.ContainsFocus && length==0 && caret){using var pen=new Pen(AppTheme.Text);g.DrawLine(pen,left-scroll,y+2,left-scroll,y+font.GetHeight(g)-2);}
        }
        private int Hit(int x)
        {
            using var g=CreateGraphics();g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            for(int i=0;i<owner.editor.Text.Length;i++)if(x+scroll-Origin(g)<(Advance(g,i)+Advance(g,i+1))/2)return i;
            return owner.editor.Text.Length;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left)return;owner.editor.Focus();anchor=Hit(e.X);Input?.Select(anchor,0);Capture=true;caret=true;Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {if(!Capture)return;int end=Hit(e.X);Input?.Select(Math.Min(anchor,end),Math.Abs(end-anchor));Invalidate();}
        protected override void OnMouseUp(MouseEventArgs e){Capture=false;base.OnMouseUp(e);}
        protected override void OnMouseDoubleClick(MouseEventArgs e){Input?.SelectAll();Invalidate();}
        protected override void OnMouseWheel(MouseEventArgs e)=>owner.OnMouseWheel(e);
        protected override void Dispose(bool disposing){if(disposing)blink.Dispose();base.Dispose(disposing);}
    }
    private sealed class NumberEditor(RoundedNumber owner) : NumericUpDown
    {
        protected override void OnMouseWheel(MouseEventArgs e)=>owner.OnMouseWheel(e);
    }
    private sealed class ArrowSurface : Control
    {
        private readonly RoundedNumber owner;
        internal ArrowSurface(RoundedNumber parent){owner=parent;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);TabStop=false;AccessibleName="Increase or decrease";}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(!owner.Enabled)return;owner.editor.Focus();if(e.Y<Height/2)owner.editor.UpButton();else owner.editor.DownButton();}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.Clear(Parent?.BackColor ?? AppTheme.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
            
            
            using var pen=new Pen(owner.Enabled?AppTheme.Text:Color.Gray,1.5f*DeviceDpi/96f);float x=Width/2f,dy=4*DeviceDpi/96f;
            foreach(int direction in new[]{-1,1}){float y=Height*(direction<0?.28f:.72f);g.DrawLines(pen,[new PointF(x-dy,y-direction*dy/2),new PointF(x,y+direction*dy/2),new PointF(x+dy,y-direction*dy/2)]);}
        }
    }
}
