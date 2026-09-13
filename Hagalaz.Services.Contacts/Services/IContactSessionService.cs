namespace Hagalaz.Services.Contacts.Services
{
    public interface IContactSessionService
    {
        public Task AddLobbySession(int worldId, uint masterId, long sessionGeneration, string connectionId);
        public Task AddWorldSession(int worldId, uint masterId, long sessionGeneration, string connectionId);
        public Task RemoveSession(uint masterId, long sessionGeneration, string connectionId);
        public Task RemoveWorldSessions(int worldId);
    }
}
