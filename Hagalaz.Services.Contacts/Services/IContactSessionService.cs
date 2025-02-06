namespace Hagalaz.Services.Contacts.Services
{
    public interface IContactSessionService
    {
        public Task AddLobbySession(int worldId, uint masterId);
        public Task AddWorldSession(int worldId, uint masterId);
        public Task RemoveSession(uint masterId);
    }
}
