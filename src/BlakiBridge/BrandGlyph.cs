using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace BlakiBridge;
internal static class BrandGlyph
{
    internal static void Draw(Graphics g,RectangleF bounds)
    {
        var state=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/64,bounds.Height/64);g.SmoothingMode=SmoothingMode.AntiAlias;
        using var ink=new Pen(AppTheme.Text,4){LineJoin=LineJoin.Round};
        using var first=AppTheme.Round(new RectangleF(5,5,28,28),2);using var second=AppTheme.Round(new RectangleF(31,31,28,28),2);
        g.DrawPath(ink,first);g.DrawPath(ink,second);
        using var bridge=new Pen(AppTheme.Accent,6){StartCap=LineCap.Round,EndCap=LineCap.Round};
        g.DrawBezier(bridge,new PointF(19,19),new PointF(35,19),new PointF(29,45),new PointF(45,45));g.Restore(state);
    }
    internal static void ExportStoreAssets(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach(var asset in new[]{("Square44x44Logo",44),("Square150x150Logo",150),("StoreLogo",50)})
        foreach(int scale in new[]{100,125,150,200,400}){int side=(asset.Item2*scale+50)/100;using var bmp=new Bitmap(side,side);using(var g=Graphics.FromImage(bmp)){g.Clear(Color.Transparent);float inset=side*.08f;Draw(g,new RectangleF(inset,inset,side-2*inset,side-2*inset));}bmp.Save(Path.Combine(directory,$"{asset.Item1}.scale-{scale}.png"),ImageFormat.Png);}
        // Shell icons are unplated; installer artwork has its own opaque background.
        foreach(int side in new[]{16,20,24,30,32,36,40,44,48,60,64,72,80,96,128,256})
        {
            using var bmp=new Bitmap(side,side);
            using(var g=Graphics.FromImage(bmp)){g.Clear(Color.Transparent);Draw(g,new RectangleF(0,0,side,side));}
            bmp.Save(Path.Combine(directory,$"Square44x44Logo.targetsize-{side}_altform-unplated.png"),ImageFormat.Png);
        }
        using var installer=new Bitmap(100,100);
        using(var g=Graphics.FromImage(installer)){g.Clear(AppTheme.Background);Draw(g,new RectangleF(8,8,84,84));}
        installer.Save(Path.Combine(directory,"InstallerLogo.png"),ImageFormat.Png);
    }
    internal static void ExportIcon(string path)
    {
        int[] sizes=[16,20,24,32,40,48,64,128,256];var images=new List<byte[]>();
        foreach(int size in sizes){using var bmp=new Bitmap(size,size);using(var g=Graphics.FromImage(bmp)){g.Clear(Color.Transparent);Draw(g,new RectangleF(0,0,size,size));}using var stream=new MemoryStream();bmp.Save(stream,ImageFormat.Png);images.Add(stream.ToArray());}
        using var file=File.Create(path);using var writer=new BinaryWriter(file);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
        for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}
        foreach(var bytes in images)writer.Write(bytes);
    }
}
