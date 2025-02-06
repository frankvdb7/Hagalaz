namespace Raido.Server
{
    public interface IRaidoHubActivator<THub> where THub : RaidoHub
    {
        THub Create();
        void Release(THub hub);
    }
}