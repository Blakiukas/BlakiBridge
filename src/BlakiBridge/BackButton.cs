namespace BlakiBridge;

internal sealed class BackButton : RoundedButton
{
    internal bool Down;
    internal bool Joined;
    internal bool Emphasized;
    internal BackButton()
    {
        Size = new Size(40,34); FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0; FlatAppearance.MouseOverBackColor = Color.FromArgb(50,50,50);
        ForeColor = Color.WhiteSmoke; BackColor = AppTheme.Background;
        RefreshLanguage();
    }
    internal void RefreshLanguage() => AccessibleName = Down ? UiText.Pick("Monitor layout", "Monitorių išdėstymas", "Расположение мониторов") : UiText.Pick("Back", "Atgal", "Назад");
    protected override void OnPaint(PaintEventArgs e)
    {
        if(Down || Joined)Chrome.Draw(this,e.Graphics,AppTheme.Background,AppTheme.Surface,AppTheme.Border,openBottom:true);
        else base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float unit = DeviceDpi / 96f, x = ClientSize.Width / 2f, y = ClientSize.Height / 2f;
        using var pen = new Pen(ForeColor, (Emphasized ? 3f : 1.8f) * unit);
        if(Emphasized)unit *= 1.2f;
        if(Down)
        {
            e.Graphics.DrawLine(pen,x,y-8*unit,x,y+8*unit);
            e.Graphics.DrawLines(pen,[new PointF(x-7*unit,y+unit),new PointF(x,y+8*unit),new PointF(x+7*unit,y+unit)]);
            return;
        }
        e.Graphics.DrawLine(pen, x - 8*unit, y, x + 8*unit, y);
        e.Graphics.DrawLines(pen, [new PointF(x-1*unit,y-7*unit),new PointF(x-8*unit,y),new PointF(x-1*unit,y+7*unit)]);
    }
}
