namespace Raido.Server
{
    public interface IRaidoCallerContextAccessor
    {
        RaidoCallerContext Context { get; set; }
    }
}
