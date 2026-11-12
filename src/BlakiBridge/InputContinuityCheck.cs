using System.Text.Json;
namespace BlakiBridge;
internal static class InputContinuityCheck
{
 internal static void Run(string output)
 {
  var checks=new List<string>();
  void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);checks.Add(name);}
  var source=new Rectangle(1515,1440,1920,480);var target=new Rectangle(0,0,5120,1440);
  var screens=new[]{source,target,new Rectangle(-2560,0,2560,1600)};
  var map=new Transition{A="LIANLI",B="Odyssey",Side=Edge.Top,StartA=0,EndA=1919,StartB=2086,EndB=3029,SizeA=source.Size,SizeB=target.Size};
  var route=new MouseBridge.Route(source,target,Edge.Top,map,false);
  foreach(var observed in new[]{new Point(2600,79),new Point(2562,105)})
  {
   var stale=new Point(observed.X,1441);
   Check(MouseBridge.MapMove(route,stale,observed)?.Y==1438,"Legacy replay reproduces Y1438 for "+observed);
   Check(!MouseBridge.InputIsContinuous(stale,observed,screens),"Rejects stale LIANLI source "+observed);
   Check(MouseBridge.InputIsContinuous(observed,observed,screens),"Rebased next input resumes "+observed);
  }
  foreach(var edge in Enum.GetValues<Edge>())foreach(var origin in new[]{new Point(-2000,-1000),new Point(0,0),new Point(1515,1440)})
  {
   var a=new Rectangle(origin,new Size(100,100));var b=new Rectangle(origin.X+300,origin.Y+300,200,200);
   var m=new Transition{A="a",B="b",Side=edge,SizeA=a.Size,SizeB=b.Size,StartA=10,EndA=90,StartB=20,EndB=180};
   var r=new MouseBridge.Route(a,b,edge,m,false);var from=new Point(a.X+50,a.Y+50);
   var to=edge switch{Edge.Left=>new Point(a.Left-10,from.Y),Edge.Right=>new Point(a.Right+10,from.Y),Edge.Top=>new Point(from.X,a.Top-10),_=>new Point(from.X,a.Bottom+10)};
   var expected=edge switch{Edge.Left=>new Point(b.Right-2,b.Top+100),Edge.Right=>new Point(b.Left+1,b.Top+100),Edge.Top=>new Point(b.Left+100,b.Bottom-2),_=>new Point(b.Left+100,b.Top+1)};
   Check(MouseBridge.InputIsContinuous(from,from,new[]{a,b}) && MouseBridge.MapMove(r,from,to)==expected,"Normal crossing "+edge+" "+origin);
   Check(!MouseBridge.InputIsContinuous(from,new Point(b.Left+100,b.Top+100),new[]{a,b}),"Reject skipped source "+edge+" "+origin);
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{passed=true,checks}));
 }
}
