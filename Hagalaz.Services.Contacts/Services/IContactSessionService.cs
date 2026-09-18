namespace Hagalaz.Services.Contacts.Services
{
    public interface IContactSessionService
    {
        public Task AddLobbySession(int worldId, string worldInstanceId, long worldGeneration, uint masterId, long sessionGeneration, string connectionId);
        public Task AddWorldSession(int worldId, string worldInstanceId, long worldGeneration, uint masterId, long sessionGeneration, string connectionId);
        public Task RemoveSession(uint masterId, long sessionGeneration, string connectionId);
        public Task RemoveWorldSessions(int worldId, string worldInstanceId, long worldGeneration);
    }
}
