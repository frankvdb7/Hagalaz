using System;

namespace Hagalaz.Services.Contacts.Store.Model
{
    public sealed record ContactSessionContext
    {
        public ContactSessionContext(uint masterId, int worldId, string worldName)
        {
            MasterId = masterId;
            WorldId = worldId;
            WorldName = worldName;
            SessionId = Guid.NewGuid();
        }

        public uint MasterId { get; }

        public int WorldId { get; }

        public string WorldName { get; }

        public Guid SessionId { get; }
    }
}
