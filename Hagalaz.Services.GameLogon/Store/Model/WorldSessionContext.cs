using System;

namespace Hagalaz.Services.GameLogon.Store.Model
{
    public class WorldSessionContext
    {
        public int WorldId { get; }
        public Uri Address { get; }

        public WorldSessionContext(int worldId, Uri address)
        {
            WorldId = worldId;
            Address = address;
        }
    }
}