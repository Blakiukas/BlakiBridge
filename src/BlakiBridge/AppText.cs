using System.Drawing.Text;
namespace BlakiBridge;
internal static class AppText
{
    private static readonly Dictionary<(string,string,float,FontStyle,GraphicsUnit),Size> measurements=new();
    internal static Size MeasureText(string text,Font font)
    {
        var key=(text,font.Name,font.Size,font.Style,font.Unit);
        lock(measurements)
        {
            if(measurements.TryGetValue(key,out var size))return size;
            using var bitmap=new Bitmap(1,1);using var graphics=Graphics.FromImage(bitmap);
            graphics.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
            size=Size.Ceiling(graphics.MeasureString(text,font));
            if(measurements.Count>=2048)measurements.Clear();
            measurements[key]=size;return size;
        }
    }

    private static readonly Dictionary<(string,float,FontStyle,float),Rectangle> lineInk=new();
    internal static float LineTop(Graphics graphics,Font font,int height)
    {
        var key=(font.Name,font.SizeInPoints,font.Style,graphics.DpiY);
        if(!lineInk.TryGetValue(key,out var ink))
        {
            int side=Math.Max(32,(int)Math.Ceiling(font.GetHeight(graphics)*4));
            using var bitmap=new Bitmap(side,side);bitmap.SetResolution(graphics.DpiX,graphics.DpiY);
            using(var g=Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Black);g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
                g.DrawString("H0",font,Brushes.White,PointF.Empty,StringFormat.GenericTypographic);
            }
            int top=side,bottom=-1;
            for(int y=0;y<side;y++)for(int x=0;x<side;x++)if(bitmap.GetPixel(x,y).R>96){top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
            ink=bottom>=top?new Rectangle(0,top,0,bottom-top+1):new Rectangle(0,0,0,(int)font.GetHeight(graphics));
            if(lineInk.Count>128)lineInk.Clear();lineInk[key]=ink;
        }
        // Centre a stable cap/digit reference, never move the baseline for descenders.
        return (float)Math.Round((height-ink.Height)/2f-ink.Top);
    }

    internal static void DrawText(Graphics graphics,string? text,Font font,Rectangle bounds,Color color,TextFormatFlags flags=TextFormatFlags.Default)
    {
        var previous=graphics.TextRenderingHint;
        using var format=new StringFormat{Alignment=flags.HasFlag(TextFormatFlags.HorizontalCenter)?StringAlignment.Center:flags.HasFlag(TextFormatFlags.Right)?StringAlignment.Far:StringAlignment.Near,LineAlignment=flags.HasFlag(TextFormatFlags.VerticalCenter)?StringAlignment.Center:flags.HasFlag(TextFormatFlags.Bottom)?StringAlignment.Far:StringAlignment.Near,Trimming=flags.HasFlag(TextFormatFlags.EndEllipsis)?StringTrimming.EllipsisCharacter:StringTrimming.None,HotkeyPrefix=HotkeyPrefix.Hide};
        if(flags.HasFlag(TextFormatFlags.SingleLine))format.FormatFlags|=StringFormatFlags.NoWrap;
        using var brush=new SolidBrush(color);
        try
        {
            graphics.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
            if(flags.HasFlag(TextFormatFlags.SingleLine) && flags.HasFlag(TextFormatFlags.VerticalCenter))
            {
                using var single=new StringFormat(StringFormat.GenericTypographic){Alignment=format.Alignment,Trimming=format.Trimming,FormatFlags=StringFormatFlags.NoWrap};
                var line=new RectangleF(bounds.X,bounds.Y+LineTop(graphics,font,bounds.Height),bounds.Width,font.GetHeight(graphics)+4);
                graphics.DrawString(text,font,brush,line,single);
            }
            else graphics.DrawString(text,font,brush,bounds,format);
        }
        finally{graphics.TextRenderingHint=previous;}
    }
}
internal sealed class SmoothLabel : Label
{
    internal bool HighlightResources;
    internal bool CenterCapHeight;
    internal SmoothLabel(){UseCompatibleTextRendering=true;}
    protected override void OnPaint(PaintEventArgs e)
    {if(HighlightResources){ResourceText.Draw(e.Graphics,Text,Font,ClientRectangle,Padding,ForeColor,true);return;}if(CenterCapHeight){var flags=TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter;
        if(TextAlign==ContentAlignment.MiddleRight)flags|=TextFormatFlags.Right;
        AppText.DrawText(e.Graphics,Text,Font,new Rectangle(Padding.Left,Padding.Top,Width-Padding.Horizontal,Height-Padding.Vertical),ForeColor,flags);return;}
        e.Graphics.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;base.OnPaint(e);}
}
// Inline units share the numeric field's full line box, font and vertical centre.
internal sealed class InlineUnit : Control
{
    internal InlineUnit(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Background);
        e.Graphics.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
        using var format=new StringFormat(StringFormat.GenericTypographic){Alignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap};
        using var brush=new SolidBrush(ForeColor);
        float y=AppText.LineTop(e.Graphics,Font,Height);
        e.Graphics.DrawString(Text,Font,brush,new PointF(Width/2f,y),format);
    }
}

internal static class ResourceText
{
    internal static void Draw(Graphics g,string text,Font font,Rectangle bounds,Padding padding,Color normal,bool single)
    {
        g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
        using var format=new StringFormat(StringFormat.GenericTypographic){FormatFlags=StringFormatFlags.NoWrap|StringFormatFlags.MeasureTrailingSpaces};
        var spans=System.Text.RegularExpressions.Regex.Matches(text,@"(?:Vid\. CPU fone|Average background CPU|Background CPU|Средняя фоновая нагрузка|CPU|RAM)(?=:)");
        float left=bounds.Left+padding.Left,x=left,y=single?bounds.Top+AppText.LineTop(g,font,bounds.Height):bounds.Top+padding.Top;
        float right=bounds.Right-padding.Right,line=font.GetHeight(g);
        foreach(System.Text.RegularExpressions.Match token in System.Text.RegularExpressions.Regex.Matches(text,@"\n|[^\S\n]+|[^\s]+"))
        {
            if(token.Value=="\n"){x=left;y+=line;continue;}
            float width=g.MeasureString(token.Value,font,int.MaxValue,format).Width;
            if(!single&&x>left&&x+width>right){x=left;y+=line;if(string.IsNullOrWhiteSpace(token.Value))continue;}
            bool highlight=spans.Cast<System.Text.RegularExpressions.Match>().Any(m=>token.Index>=m.Index&&token.Index<m.Index+m.Length);
            using var brush=new SolidBrush(highlight?Color.FromArgb(190,190,190):normal);
            g.DrawString(token.Value,font,brush,new PointF(x,y),format);x+=width;
        }
    }
}
