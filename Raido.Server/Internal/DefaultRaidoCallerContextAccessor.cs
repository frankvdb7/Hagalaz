namespace Raido.Server.Internal
{
    internal class DefaultRaidoCallerContextAccessor : IRaidoCallerContextAccessor
    {
        public RaidoCallerContext Context { get; set; } = default!;
    }
}
