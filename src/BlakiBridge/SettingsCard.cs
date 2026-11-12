namespace BlakiBridge;
internal class RoundedSurface : Panel
{
    internal RoundedSurface(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);BackColor=AppTheme.Surface;}
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Chrome.Draw(this,e.Graphics,BackColor,BackColor,AppTheme.Border);
    }
}
internal sealed class InformationBand : Panel
{
    internal bool JoinNavigation;
    private static IEnumerable<Control> Descendants(Control root){foreach(Control child in root.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    internal InformationBand(Control content)
    {
        DoubleBuffered=true;Dock=DockStyle.Top;Height=96;Padding=new Padding(22,0,22,0);BackColor=AppTheme.Background;
        var surface=new NavigationSurface{Dock=DockStyle.Fill,Padding=new Padding(10,8,10,8)};
        content.Dock=DockStyle.Fill;content.BackColor=AppTheme.Surface;surface.Controls.Add(content);Controls.Add(surface);

    }
}
// The top edge belongs to the selected tab and its page, with one opening beneath that tab.
internal sealed class NavigationSurface : RoundedSurface
{
    protected override void OnPaintBackground(PaintEventArgs e)
    {

        float scale=DeviceDpi/96f;int radius=(int)Math.Ceiling(6*scale),stroke=Math.Max(1,(int)Math.Round(scale));
        DisplayNavigation? navigation=null;
        for(Control? scope=Parent;scope!=null && navigation==null;scope=scope.Parent)
            navigation=scope.Controls.OfType<DisplayNavigation>().FirstOrDefault()??scope.Controls.Cast<Control>().SelectMany(c=>c.Controls.OfType<DisplayNavigation>()).FirstOrDefault();
        Control? selected=navigation?.Tabs.FirstOrDefault(t=>t.Selected);
        bool endpoint=selected is null && navigation?.Back.Down==true;
        if(endpoint)selected=navigation!.Back;
        Chrome.Draw(this,e.Graphics,BackColor,BackColor,AppTheme.Border,joinedTop:true,squareTopLeft:endpoint);
        int left=radius,right=Width-radius;
        var state=e.Graphics.Save();e.Graphics.SetClip(new Rectangle(left,0,Math.Max(0,right-left),stroke));e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Background);e.Graphics.Restore(state);
        using var rule=new SolidBrush(AppTheme.Border);
        if(selected is null){e.Graphics.FillRectangle(rule,left,0,Math.Max(0,right-left),stroke);return;}
        int start=PointToClient(selected.PointToScreen(Point.Empty)).X,end=start+selected.Width;
        if(start>left)e.Graphics.FillRectangle(rule,left,0,start-left,stroke);
        if(end<right)e.Graphics.FillRectangle(rule,end,0,right-end,stroke);
    }
}
internal sealed class SettingsCard : RoundedSurface
{
    private readonly string heading;
    internal SettingsCard(string title){heading=title;Padding=new Padding(8,38,8,8);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);float s=DeviceDpi/96f;int headingHeight=(int)(36*s);
        using var border=new Pen(AppTheme.Border,s);
        e.Graphics.DrawLine(border,s,headingHeight,Width-s,headingHeight);
        AppText.DrawText(e.Graphics,heading,Font,new Rectangle((int)(14*s),0,Width-(int)(28*s),headingHeight),AppTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    }
}

internal sealed class BufferedTableLayout : TableLayoutPanel
{
    internal BufferedTableLayout(){DoubleBuffered=true;SetStyle(ControlStyles.AllPaintingInWmPaint,true);}
}
