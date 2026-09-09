namespace Hagalaz.Services.Contacts.Services
{
    public interface IContactSessionService
    {
        public Task AddLobbySession(int worldId, uint masterId, string connectionId);
        public Task AddWorldSession(int worldId, uint masterId, string connectionId);
        public Task RemoveSession(uint masterId, string connectionId);
        public Task RemoveWorldSessions(int worldId);
    }
}
