namespace BlakiBridge;
// No timer or user switch: errors open a bounded, rate-limited observation window.
internal sealed class AutomaticDiagnostics
{
    private long until, nextAllowed;
    private int remaining;
    internal bool Active(long now)=>now<until && remaining>0;
    internal bool Trigger(long now)
    {
        if(now<nextAllowed)return false;
        until=now+15000;nextAllowed=now+300000;remaining=1024;return true;
    }
    internal bool Take(long now){if(!Active(now))return false;remaining--;return true;}
}
