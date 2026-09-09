using System;

namespace Hagalaz.Services.Contacts.Store.Model
{
    public sealed record ContactSessionContext
    {
        public ContactSessionContext(uint masterId, int worldId, string worldName, string connectionId)
        {
            MasterId = masterId;
            WorldId = worldId;
            WorldName = worldName;
            ConnectionId = connectionId;
            SessionId = Guid.NewGuid();
        }

        public uint MasterId { get; }

        public int WorldId { get; }

        public string WorldName { get; }

        public string ConnectionId { get; }

        public Guid SessionId { get; }
    }
}
