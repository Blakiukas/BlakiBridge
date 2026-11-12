namespace BlakiBridge;

public enum Edge { Left, Right, Top, Bottom }

public sealed class Transition
{
    public string A { get; set; } = "";
    public string B { get; set; } = "";
    public Edge Side { get; set; }
    public int StartA { get; set; }
    public int EndA { get; set; }
    public int StartB { get; set; }
    public int EndB { get; set; }
    public Size SizeA { get; set; }
    public Size SizeB { get; set; }
    public static bool Vertical(Edge edge) => edge is Edge.Left or Edge.Right;
    public static Edge Opposite(Edge edge) => edge switch { Edge.Left => Edge.Right, Edge.Right => Edge.Left, Edge.Top => Edge.Bottom, _ => Edge.Top };
    public bool Valid => A != B && A.Length > 0 && B.Length > 0 && Enum.IsDefined(Side)
        && StartA >= 0 && StartB >= 0 && EndA > StartA && EndB > StartB
        && EndA < (Vertical(Side) ? SizeA.Height : SizeA.Width)
        && EndB < (Vertical(Side) ? SizeB.Height : SizeB.Width);
    public double Map(double value, bool reverse = false)
    {
        if (!Valid) throw new InvalidOperationException("Invalid transition.");
        var (start, end, target, targetEnd) = reverse ? (StartB, EndB, StartA, EndA) : (StartA, EndA, StartB, EndB);
        if (!double.IsFinite(value) || value < start || value > end) throw new ArgumentOutOfRangeException(nameof(value));
        return target + (value - start) * (targetEnd - target) / (end - start);
    }
}
