namespace BlakiBridge;

// One queued crossing at a time. Cancel invalidates even an already-posted callback.
internal sealed class DeferredWarp
{
    private long generation;
    internal bool Pending { get; private set; }
    internal long QueuedAt { get; private set; }
    internal bool Queue(Point target, Action<Action> post, Action<Point> apply)
    {
        if (Pending) return false;
        Pending = true; QueuedAt = Environment.TickCount64;
        long ticket = ++generation;
        try
        {
            post(() => {
                if (!Pending || ticket != generation) return;
                try { apply(target); }
                finally { if (ticket == generation) Pending = false; }
            });
            return true;
        }
        catch { Cancel(); throw; }
    }
    internal void Cancel() { generation++; Pending = false; }
}
