using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
namespace BlakiBridge;
internal static class Chrome
{
    private sealed class Cache { internal string Key=""; internal Bitmap? Image; }
    private static readonly ConditionalWeakTable<Control,Cache> caches=new();
    internal static void Draw(Control owner,Graphics target,Color top,Color bottom,Color border,bool openTop=false,bool openBottom=false,float thickness=1,bool bottomRule=false,bool joinedTop=false,bool squareTopLeft=false)
    {
        if(owner.Width<=0 || owner.Height<=0)return;
        float scale=owner.DeviceDpi/96f,stroke=Math.Max(1,(float)Math.Round(thickness*scale));
        Color outside=owner.Parent?.BackColor??AppTheme.Background;
        int edge=(int)Math.Ceiling(6*scale+stroke+2);
        int width=Math.Min(owner.Width,2*edge+1),height=top==bottom?Math.Min(owner.Height,2*edge+1):owner.Height;
        string key=$"{width}x{height}/{scale}/{top.ToArgb()}/{bottom.ToArgb()}/{border.ToArgb()}/{outside.ToArgb()}/{openTop}/{openBottom}/{stroke}/{bottomRule}/{joinedTop}/{squareTopLeft}";
        var cache=caches.GetValue(owner,c=>{var result=new Cache();c.Disposed+=(_,_)=>result.Image?.Dispose();return result;});
        if(cache.Key!=key)
        {
            const int sample=4;
            using var high=new Bitmap(width*sample,height*sample,PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(high))
            {
                g.Clear(outside);g.ScaleTransform(sample,sample);g.TranslateTransform(-.5f/sample,-.5f/sample);g.SmoothingMode=SmoothingMode.AntiAlias;
                // Extend open edges outside the raster: antialiasing must never make a seam row.
                var r=new RectangleF(stroke/2,openTop?-2:stroke/2,width-stroke,height-stroke+(openTop?2+stroke/2:0)+(openBottom?2+stroke/2:0));
                using var path=openTop?AppTheme.OpenTop(r,6*scale):AppTheme.Round(r,6*scale);
                if(openBottom)
                {
                    path.Reset();float radius=6*scale;
                    path.AddLine(r.Left,r.Bottom,r.Left,r.Top+radius);path.AddArc(r.Left,r.Top,2*radius,2*radius,180,90);
                    path.AddArc(r.Right-2*radius,r.Top,2*radius,2*radius,270,90);path.AddLine(r.Right,r.Top+radius,r.Right,r.Bottom);
                }
                using var fill=new LinearGradientBrush(new Rectangle(0,0,width,height),top,bottom,90f);
                fill.InterpolationColors=new ColorBlend{Colors=[top,bottom,bottom],Positions=[0,.90f,1]};
                if(squareTopLeft){using var corner=new SolidBrush(top);g.FillRectangle(corner,0,0,6*scale,6*scale);}
                g.FillPath(fill,path);
                if(joinedTop)
                {
                    float d=12*scale;path.Reset();
                    if(squareTopLeft)path.AddLine(r.Left,-2,r.Left,r.Top+d/2);else path.AddArc(r.Left,r.Top,d,d,270,-90);path.AddLine(r.Left,r.Top+d/2,r.Left,r.Bottom-d/2);
                    path.AddArc(r.Left,r.Bottom-d,d,d,180,-90);path.AddArc(r.Right-d,r.Bottom-d,d,d,90,-90);
                    path.AddLine(r.Right,r.Bottom-d/2,r.Right,r.Top+d/2);path.AddArc(r.Right-d,r.Top,d,d,0,-90);
                }
                using var pen=new Pen(border,stroke);g.DrawPath(pen,path);
                if(bottomRule)g.DrawLine(pen,r.Left,height-stroke/2,r.Right,height-stroke/2);
            }
            var result=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(result))
            {
                g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                using var attributes=new ImageAttributes();attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(high,new Rectangle(0,0,result.Width,result.Height),0,0,high.Width,high.Height,GraphicsUnit.Pixel,attributes);
            }
            cache.Image?.Dispose();cache.Image=result;cache.Key=key;
        }
        // Keep corner pixels unchanged; stretch only uniform centre strips.
        int ex=Math.Min(edge,width/2),ey=Math.Min(edge,height/2);
        int[] sx=[0,ex,width-ex,width],sy=[0,ey,height-ey,height];
        int[] dx=[0,ex,owner.Width-ex,owner.Width],dy=[0,ey,owner.Height-ey,owner.Height];
        var state=target.Save();target.InterpolationMode=InterpolationMode.NearestNeighbor;target.PixelOffsetMode=PixelOffsetMode.Half;
        for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            if(dx[x+1]>dx[x] && dy[y+1]>dy[y])target.DrawImage(cache.Image!,new Rectangle(dx[x],dy[y],dx[x+1]-dx[x],dy[y+1]-dy[y]),new Rectangle(sx[x],sy[y],sx[x+1]-sx[x],sy[y+1]-sy[y]),GraphicsUnit.Pixel);
        target.Restore(state);
    }
}
