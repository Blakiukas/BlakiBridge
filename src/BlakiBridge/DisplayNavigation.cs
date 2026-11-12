namespace BlakiBridge;
internal sealed class DisplayNavigation : Panel
{
    internal readonly BackButton Back=new(){Joined=true,Height=44,Margin=new Padding(0,0,8,0)};
    internal readonly PageTabButton[] Tabs=[new(){AutoSize=true},new(){AutoSize=true},new(){AutoSize=true}];
    private readonly SmoothLabel identity=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,AutoEllipsis=true,ForeColor=AppTheme.Accent,Font=new Font("Segoe UI",12,FontStyle.Bold),Padding=new Padding(12,0,22,0)};
    internal DisplayNavigation()
    {
        DoubleBuffered=true;Dock=DockStyle.Top;Height=52;Padding=new Padding(22,8,0,0);
        Controls.Add(Back);foreach(var tab in Tabs)Controls.Add(tab);
        Controls.Add(identity);identity.Dock=DockStyle.None;RefreshText();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int x=Padding.Left,gap=(int)(4*DeviceDpi/96f),height=(int)(44*DeviceDpi/96f);
        Back.SetBounds(x,Padding.Top,(int)(40*DeviceDpi/96f),height);x=Back.Right+gap*2;
        foreach(var tab in Tabs){int width=tab.GetPreferredSize(Size.Empty).Width;tab.SetBounds(x,Padding.Top,width,height);x+=width+gap;}
        identity.SetBounds(x,Padding.Top,Math.Max(0,ClientSize.Width-x),height);
    }
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if(Visible && IsHandleCreated)BeginInvoke((Action)(()=>{if(!IsDisposed)Invalidate(true);}));
    }
    private string monitorName="";
    internal void RefreshText(){SetMonitor(monitorName);Tabs[0].Text=UiText.Pick("Dimensions","Matmenys","Размеры");Tabs[1].Text=UiText.Pick("Mouse speed","Pelės greitis","Скорость мыши");Tabs[2].Text=UiText.T("Physical gap");}
    internal void SetMonitor(string name){monitorName=name;identity.Text=string.IsNullOrEmpty(name)?UiText.Pick("Select a monitor","Pasirinkite monitorių","Выберите монитор"):name;identity.ForeColor=string.IsNullOrEmpty(name)?Color.Silver:AppTheme.Accent;identity.AccessibleName=identity.Text;}
    internal void SelectTab(int index){for(int i=0;i<Tabs.Length;i++){Tabs[i].Selected=i==index;Tabs[i].Invalidate();}}
}
