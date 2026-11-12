using System.Runtime.InteropServices;
namespace BlakiBridge;

// One horizontal row with a common vertical centre, independent of text length.
internal sealed class LayoutRow : FlowLayoutPanel
{
    internal bool ResizeGrip;
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(!ResizeGrip||FindForm()?.WindowState==FormWindowState.Maximized)return;float s=DeviceDpi/96f;using var pen=new Pen(Color.FromArgb(153,153,153),s);for(int i=0;i<3;i++){float d=(5+i*4)*s;e.Graphics.DrawLine(pen,Width-d,Height-3*s,Width-3*s,Height-d);}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(ResizeGrip)Cursor=e.X>Width-18*DeviceDpi/96f&&e.Y>Height-18*DeviceDpi/96f?Cursors.SizeNWSE:Cursors.Default;}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(ResizeGrip&&e.Button==MouseButtons.Left&&Cursor==Cursors.SizeNWSE&&FindForm() is Form f){ReleaseCapture();SendMessage(f.Handle,0xA1,(IntPtr)17,IntPtr.Zero);}}
    [DllImport("user32.dll")]private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
    internal LayoutRow() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); WrapContents = false; }
    public override Size GetPreferredSize(Size proposedSize)
    {
        int width = Padding.Horizontal, height = 0;
        foreach (Control child in Controls)
        {
            if (!child.Visible) continue;
            Size size = child.AutoSize ? child.GetPreferredSize(Size.Empty) : child.Size;
            width += size.Width + child.Margin.Horizontal;
            height = Math.Max(height, size.Height + child.Margin.Vertical);
        }
        return new Size(width, height + Padding.Vertical);
    }
    private static readonly System.Windows.Forms.Layout.LayoutEngine rowEngine = new RowEngine();
    public override System.Windows.Forms.Layout.LayoutEngine LayoutEngine => rowEngine;
    private sealed class RowEngine : System.Windows.Forms.Layout.LayoutEngine
    {
        public override bool Layout(object container, LayoutEventArgs e)
        {
            var row=(LayoutRow)container;
            int x=row.Padding.Left;
            foreach(Control child in row.Controls)
            {
                if(!child.Visible)continue;
                var size=child.AutoSize?child.GetPreferredSize(Size.Empty):child.Size;
                x+=child.Margin.Left;
                child.SetBounds(x,row.Padding.Top+(row.ClientSize.Height-row.Padding.Vertical-size.Height)/2,size.Width,size.Height);
                x+=size.Width+child.Margin.Right;
            }
            return row.AutoSize;
        }
    }

}

internal sealed class WindowHeader : Panel
{
    private readonly Form owner;
    private readonly Control language;
    private readonly BrandHeader brand = new();
    private readonly RoundedSurface captionFrame = new(){BackColor=AppTheme.Background};
    private readonly Button minimize = new CaptionButton("—", "Minimize");
    private readonly Button maximize = new CaptionButton("□", "Maximize / Restore");
    private readonly Button close = new CaptionButton("×", "Close");
    internal WindowHeader(Form form, Control selector)
    {
        owner = form; language = selector;
        var resizeInput=new ResizeInput(form); Application.AddMessageFilter(resizeInput);
        Disposed+=(_,_)=>Application.RemoveMessageFilter(resizeInput); DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true);
        Controls.AddRange([brand, language, captionFrame]);
        captionFrame.Controls.AddRange([minimize,maximize,close]);
        minimize.Click += (_, _) => owner.WindowState = FormWindowState.Minimized;
        maximize.Click += (_, _) => ToggleMaximize();
        close.Click += (_, _) => owner.Close();
        brand.MouseDown += DragWindow; MouseDown += DragWindow;
        brand.DoubleClick += (_, _) => ToggleMaximize();
        DoubleClick += (_, _) => ToggleMaximize();
    }
    private void ToggleMaximize() => owner.WindowState = owner.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
    private void DragWindow(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture(); SendMessage(owner.Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int unit = (int)(46 * DeviceDpi / 96f), inset = (int)(22 * DeviceDpi / 96f);
        int frameWidth=unit*3, frameHeight=language.Height;
        captionFrame.SetBounds(Width-inset-frameWidth,(Height-frameHeight)/2,frameWidth,frameHeight);
        int pad=(int)(6*DeviceDpi/96f), inner=(frameWidth-pad*2)/3;
        int x=pad;
        foreach(var button in new[]{minimize,maximize,close})
        {button.SetBounds(x,1,inner,Math.Max(1,frameHeight-2));x+=inner;}
        language.SetBounds(captionFrame.Left-inset-language.Width,(Height-language.Height)/2,language.Width,language.Height);
        brand.SetBounds(0,0,Math.Max(0,language.Left-inset),Height);
    }
    internal static bool HandleFrame(Form form, ref Message m)
    {
        // Keep the Windows resizable frame and system menu, removing only its duplicate caption.
        if (m.Msg == 0x85) { m.Result = IntPtr.Zero; return true; }
        if (m.Msg == 0x86) { m.Result = (IntPtr)1; return true; }
        if (m.Msg == 0x83) { m.Result = IntPtr.Zero; return true; }
        if (m.Msg == 0x84 && form.WindowState == FormWindowState.Normal)
        {
            long value = m.LParam.ToInt64();
            var pt = form.PointToClient(new Point((short)(value & 0xffff), (short)((value >> 16) & 0xffff)));
            int edge = Math.Max(5, (int)(6 * form.DeviceDpi / 96f));
            bool left = pt.X < edge, right = pt.X >= form.ClientSize.Width-edge;
            bool top = pt.Y < edge, bottom = pt.Y >= form.ClientSize.Height-edge;
            int hit = top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 0;
            if (hit != 0) { m.Result = (IntPtr)hit; return true; }
        }
        if (m.Msg == 0x24)
        {
            var info = Marshal.PtrToStructure<MinMaxInfo>(m.LParam);
            var screen = Screen.FromHandle(form.Handle);
            info.MaxPosition = new Point(screen.WorkingArea.Left-screen.Bounds.Left, screen.WorkingArea.Top-screen.Bounds.Top);
            info.MaxSize = new Point(screen.WorkingArea.Width,screen.WorkingArea.Height);
            info.MinTrackSize = new Point(form.MinimumSize.Width,form.MinimumSize.Height);
            Marshal.StructureToPtr(info,m.LParam,false);
            m.Result=IntPtr.Zero; return true;
        }
        return false;
    }
    // Child controls fill the client frame. Forward their edge presses to Windows sizing.
    private sealed class ResizeInput(Form owner) : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            if(m.Msg is not (0x200 or 0x201) || owner.WindowState!=FormWindowState.Normal || !owner.Visible)return false;
            var control=Control.FromHandle(m.HWnd);
            var ancestor=control; while(ancestor!=null && ancestor!=owner)ancestor=ancestor.Parent;
            if(ancestor!=owner)return false;
            var pt=owner.PointToClient(Cursor.Position); int edge=Math.Max(5,6*owner.DeviceDpi/96);
            bool left=pt.X>=0 && pt.X<edge,right=pt.X>=owner.ClientSize.Width-edge && pt.X<owner.ClientSize.Width;
            bool top=pt.Y>=0 && pt.Y<edge,bottom=pt.Y>=owner.ClientSize.Height-edge && pt.Y<owner.ClientSize.Height;
            int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
            if(hit==0)return false;
            if(m.Msg==0x200)
            {
                Cursor.Current=hit is 13 or 17?Cursors.SizeNWSE:hit is 14 or 16?Cursors.SizeNESW:hit is 10 or 11?Cursors.SizeWE:Cursors.SizeNS;
                return false;
            }
            ReleaseCapture(); SendMessage(owner.Handle,0xA1,(IntPtr)hit,IntPtr.Zero);return true;
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo { public Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
    private sealed class CaptionButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(ClientRectangle.Contains(PointToClient(Cursor.Position))?AppTheme.Surface:AppTheme.Background);
            float scale=DeviceDpi/96f,stroke=Math.Max(1,(float)Math.Round(scale));
            float side=(float)Math.Round(10*scale),x=(float)Math.Floor((Width-side)/2)+stroke/2,y=(float)Math.Floor((Height-side)/2)+stroke/2;
            using var pen=new Pen(AppTheme.Text,stroke);
            if(AccessibleName=="Minimize")e.Graphics.DrawLine(pen,x,y+side/2,x+side,y+side/2);
            else if(AccessibleName=="Close"){e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;e.Graphics.DrawLine(pen,x,y,x+side,y+side);e.Graphics.DrawLine(pen,x+side,y,x,y+side);}
            else e.Graphics.DrawRectangle(pen,x,y,side,side);
        }
        internal CaptionButton(string text, string accessible)
        {
            Text=text; AccessibleName=accessible; FlatStyle=FlatStyle.Flat; FlatAppearance.BorderSize=0;
            FlatAppearance.MouseOverBackColor=AppTheme.Surface; BackColor=AppTheme.Background;
            ForeColor=AppTheme.Text; TabStop=false; Font=new Font("Segoe UI",14);
        }
    }
}
