namespace BlakiBridge;
public sealed class MonitorLink
{
    public string A {get;set;}="";
    public string B {get;set;}="";
    public Edge Side {get;set;}
    public MonitorLink Copy()=>new(){A=A,B=B,Side=Side};
}
internal static class MonitorTopology
{
    internal static List<MonitorLink> Suggest(MonitorCanvas canvas)
    {
        var result=new List<MonitorLink>();
        foreach(var a in canvas.Monitors)
        {
            MonitorLink? best=null;float score=float.MaxValue;
            var x=canvas.DiagramBounds(a);
            foreach(var b in canvas.Monitors.Where(m=>m!=a))
            {
                var y=canvas.DiagramBounds(b);
                float overlapY=Math.Min(x.Bottom,y.Bottom)-Math.Max(x.Top,y.Top),overlapX=Math.Min(x.Right,y.Right)-Math.Max(x.Left,y.Left);
                void Candidate(Edge side,float distance){if(distance>=-1 && distance<score){score=Math.Max(0,distance);best=new(){A=a.DeviceName,B=b.DeviceName,Side=side};}}
                if(overlapY>0){Candidate(Edge.Right,y.Left-x.Right);Candidate(Edge.Left,x.Left-y.Right);}
                if(overlapX>0){Candidate(Edge.Bottom,y.Top-x.Bottom);Candidate(Edge.Top,x.Top-y.Bottom);}
            }
            if(best!=null && !result.Any(t=>GapSettings.Key(t.A,t.B)==GapSettings.Key(best.A,best.B)))result.Add(best);
        }
        return result;
    }
    internal static List<Transition> Build(MonitorCanvas canvas,IEnumerable<MonitorLink> links,IEnumerable<Transition> previous)
    {
        var result=new List<Transition>();var old=previous.ToList();
        foreach(var link in links.DistinctBy(t=>GapSettings.Key(t.A,t.B)))
        {
            var a=canvas.Monitors.FirstOrDefault(m=>m.DeviceName==link.A);var b=canvas.Monitors.FirstOrDefault(m=>m.DeviceName==link.B);
            if(a==null || b==null || a==b || !Enum.IsDefined(link.Side))continue;
            var match=old.FirstOrDefault(t=>t.Valid && ((t.A==link.A && t.B==link.B && t.Side==link.Side && t.SizeA==a.Bounds.Size && t.SizeB==b.Bounds.Size)||(t.A==link.B && t.B==link.A && t.Side==Transition.Opposite(link.Side) && t.SizeA==b.Bounds.Size && t.SizeB==a.Bounds.Size)));
            if(match!=null){result.Add(match);continue;}
            bool vertical=Transition.Vertical(link.Side);var ra=canvas.DiagramBounds(a);var rb=canvas.DiagramBounds(b);
            float pa=vertical?ra.Top:ra.Left,pb=vertical?rb.Top:rb.Left,la=vertical?ra.Height:ra.Width,lb=vertical?rb.Height:rb.Width;
            int na=vertical?a.Bounds.Height:a.Bounds.Width,nb=vertical?b.Bounds.Height:b.Bounds.Width;
            float start=Math.Max(pa,pb),end=Math.Min(pa+la,pb+lb);
            int sa=0,sb=0,ea=na-1,eb=nb-1;
            if(end>start && la>0 && lb>0){sa=Math.Clamp((int)Math.Round((start-pa)/la*(na-1)),0,na-2);ea=Math.Clamp((int)Math.Round((end-pa)/la*(na-1)),sa+1,na-1);sb=Math.Clamp((int)Math.Round((start-pb)/lb*(nb-1)),0,nb-2);eb=Math.Clamp((int)Math.Round((end-pb)/lb*(nb-1)),sb+1,nb-1);}
            result.Add(new(){A=link.A,B=link.B,Side=link.Side,SizeA=a.Bounds.Size,SizeB=b.Bounds.Size,StartA=sa,EndA=ea,StartB=sb,EndB=eb});
        }
        return result;
    }
}
internal sealed class ConnectionsPage : Panel
{
    internal readonly List<MonitorLink> Links;
    internal readonly Dictionary<string,GapSettings> Gaps;
    private readonly string selected;
    private readonly IReadOnlyList<MonitorModel> monitors;
    private readonly ComboBox neighbor=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top,FlatStyle=FlatStyle.Flat,BackColor=AppTheme.Background,ForeColor=AppTheme.Text};
    private readonly ComboBox side=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top,FlatStyle=FlatStyle.Flat,BackColor=AppTheme.Background,ForeColor=AppTheme.Text};
    private readonly CheckBox connected=new BridgeSwitch{AutoSize=true};
    private readonly RoundedNumber distance=new(){Minimum=0,Maximum=2000,DecimalPlaces=1,Width=130};
    private readonly Panel preview=new(){Dock=DockStyle.Fill};
    private bool binding;
    internal ConnectionsPage(string selected,IReadOnlyList<MonitorModel> monitors,List<MonitorLink> links,Dictionary<string,GapSettings> gaps)
    {
        this.selected=selected;this.monitors=monitors;
        foreach(var choice in new[]{neighbor,side})
        {
            choice.DrawMode=DrawMode.OwnerDrawFixed;
            choice.DrawItem+=(_,e)=>{using var fill=new SolidBrush(e.State.HasFlag(DrawItemState.Selected)?Color.FromArgb(48,48,48):AppTheme.Background);e.Graphics.FillRectangle(fill,e.Bounds);if(e.Index>=0)AppText.DrawText(e.Graphics,choice.Items[e.Index]?.ToString(),choice.Font,e.Bounds,AppTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.EndEllipsis);};
        }
        Links=links.Select(l=>l.Copy()).ToList();Gaps=gaps.ToDictionary(k=>k.Key,k=>new GapSettings{EdgeDistanceMm=k.Value.EdgeDistanceMm,Millimeters=k.Value.Millimeters,Compensate=k.Value.Compensate});
        Dock=DockStyle.Fill;BackColor=AppTheme.Background;
        var info=new SmoothLabel{Dock=DockStyle.Top,Height=96,Padding=new Padding(2,4,2,4),TextAlign=ContentAlignment.MiddleLeft,BackColor=AppTheme.Surface,Text=UiText.Pick("Choose a neighboring monitor and its position relative to this screen. The same pair is used for adjustment and physical distance. Save keeps your choice.","Pasirinkite kaimyninį monitorių ir jo vietą šio ekrano atžvilgiu. Ši pora naudojama ir reguliavimui, ir fiziniam tarpui. Pasirinkimą patvirtinkite išsaugodami.","Выберите соседний монитор и его положение относительно этого экрана. Эта пара используется для калибровки и расстояния. Подтвердите выбор сохранением.")};
        var body=new BufferedTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(22,12,22,20)};
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var card=new SettingsCard(UiText.T("Physical gap")){Dock=DockStyle.Fill,Margin=new Padding(0,0,8,0)};
        var previewCard=new SettingsCard(UiText.Pick("Preview","Peržiūra","Просмотр")){Dock=DockStyle.Fill,Margin=new Padding(8,0,0,0)};
        var rows=new BufferedTableLayout{Dock=DockStyle.Top,AutoSize=true,ColumnCount=3,Padding=new Padding(12,8,12,0)};
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,56));rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,44));rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,52));
        string Name(MonitorModel m)=>$"{monitors.ToList().IndexOf(m)+1} · {m.ModelName}";
        void Row(string text,Control input,int row)
        {
            rows.Controls.Add(new SmoothLabel{Text=text,AutoSize=true,Margin=new Padding(0,5,0,6)},0,row);
            input.AccessibleName=text;Control field=input is ComboBox combo?new ChoiceField(combo):input;
            field.Margin=new Padding(3,0,3,8);rows.Controls.Add(field,1,row);
            if(input==distance){input.Dock=DockStyle.Top;rows.Controls.Add(new SmoothLabel{Text="mm",Height=32,Dock=DockStyle.Top,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(7,0,0,0),ForeColor=Color.Silver},2,row);}
            else rows.SetColumnSpan(field,2);
        }
        Row(UiText.T("Display"),new SmoothLabel{Text=Name(monitors.First(m=>m.DeviceName==selected)),AutoSize=true},0);
        foreach(var m in monitors.Where(m=>m.DeviceName!=selected))neighbor.Items.Add(Name(m));
        side.Items.AddRange(new object[]{UiText.Pick("Left","Kairėje","Слева"),UiText.Pick("Right","Dešinėje","Справа"),UiText.Pick("Above","Viršuje","Сверху"),UiText.Pick("Below","Apačioje","Снизу")});
        Row(UiText.Pick("Neighbor","Kaimynas","Сосед"),neighbor,1);Row(UiText.Pick("Position","Vieta","Положение"),side,2);
        connected.Text=UiText.Pick("Use this pair","Naudoti porą","Использовать пару");Row("",connected,3);
        Row(UiText.Pick("Image-to-image","Tarp vaizdų","Между экранами"),distance,4);
        card.Controls.Add(rows);previewCard.Controls.Add(preview);body.Controls.Add(card,0,0);body.Controls.Add(previewCard,1,0);Controls.AddRange([body,new InformationBand(info)]);
        neighbor.SelectedIndexChanged+=(_,_)=>LoadPair();side.SelectedIndexChanged+=(_,_)=>Change();connected.CheckedChanged+=(_,_)=>Change();distance.ValueChanged+=(_,_)=>Change();
        var initial=Links.FirstOrDefault(l=>l.A==selected || l.B==selected);string? target=initial==null?null:initial.A==selected?initial.B:initial.A;
        int index=monitors.Where(m=>m.DeviceName!=selected).ToList().FindIndex(m=>m.DeviceName==target);if(neighbor.Items.Count>0)neighbor.SelectedIndex=Math.Max(0,index);
    }
    private string Other=>monitors.Where(m=>m.DeviceName!=selected).ElementAt(neighbor.SelectedIndex).DeviceName;
    private void LoadPair()
    {
        if(neighbor.SelectedIndex<0)return;binding=true;
        var link=Links.FirstOrDefault(l=>GapSettings.Key(l.A,l.B)==GapSettings.Key(selected,Other));
        side.SelectedIndex=(int)(link==null?Edge.Right:link.A==selected?link.Side:Transition.Opposite(link.Side));
        connected.Checked=link!=null;distance.Value=Gaps.GetValueOrDefault(GapSettings.Key(selected,Other))?.EdgeDistanceMm??0;
        distance.Enabled=connected.Checked;binding=false;DrawPair();
    }
    internal void CommitCurrent()=>Change();
    private void Change()
    {
        if(binding || neighbor.SelectedIndex<0 || side.SelectedIndex<0)return;
        string key=GapSettings.Key(selected,Other);int position=Links.FindIndex(l=>GapSettings.Key(l.A,l.B)==key);Links.RemoveAll(l=>GapSettings.Key(l.A,l.B)==key);
        if(connected.Checked){Links.Insert(position<0?Links.Count:Math.Min(position,Links.Count),new(){A=selected,B=Other,Side=(Edge)side.SelectedIndex});Gaps[key]=new(){EdgeDistanceMm=distance.Value};}
        distance.Enabled=connected.Checked;DrawPair();
    }
    private void DrawPair()
    {
        foreach(Control child in preview.Controls.Cast<Control>().ToArray())child.Dispose();
        var edge=(Edge)Math.Max(0,side.SelectedIndex);bool reverse=edge is Edge.Left or Edge.Top;
        preview.Controls.Add(new GapDiagram(monitors,reverse?Other:selected,reverse?selected:Other,edge is Edge.Top or Edge.Bottom){Dock=DockStyle.Fill});
    }
}
