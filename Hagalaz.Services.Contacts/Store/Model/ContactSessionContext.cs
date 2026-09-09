namespace Hagalaz.Services.Contacts.Store.Model
{
    public sealed record ContactSessionContext
    {
        public ContactSessionContext(uint masterId, int worldId, string worldName, long sessionGeneration, string connectionId)
        {
            MasterId = masterId;
            WorldId = worldId;
            WorldName = worldName;
            SessionGeneration = sessionGeneration;
            ConnectionId = connectionId;
        }

        public uint MasterId { get; }

        public int WorldId { get; }

        public string WorldName { get; }

        public long SessionGeneration { get; }

        public string ConnectionId { get; }
    }
}
