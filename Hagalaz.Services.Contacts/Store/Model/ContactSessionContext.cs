namespace Hagalaz.Services.Contacts.Store.Model
{
    public sealed record ContactSessionContext
    {
        public ContactSessionContext(
            uint masterId,
            int worldId,
            string worldName,
            long sessionGeneration,
            string connectionId,
            string worldInstanceId = "",
            long worldGeneration = 0,
            Guid? sessionId = null)
        {
            MasterId = masterId;
            WorldId = worldId;
            WorldName = worldName;
            SessionGeneration = sessionGeneration;
            ConnectionId = connectionId;
            WorldInstanceId = worldInstanceId;
            WorldGeneration = worldGeneration;
            SessionId = sessionId ?? Guid.NewGuid();
        }

        public uint MasterId { get; }

        public int WorldId { get; }

        public string WorldName { get; }

        public long SessionGeneration { get; }

        public string ConnectionId { get; }

        public string WorldInstanceId { get; }

        public long WorldGeneration { get; }

        public Guid SessionId { get; }

        public long PresenceVersion { get; internal set; }
    }
}
