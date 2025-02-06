using System.Collections.Generic;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class MapRegionPartUpdateMessage : RaidoMessage
    {
        public int LocalX { get; init; }
        public int LocalY { get; init; }
        public int Z { get; init; }
        public bool FullUpdate { get; init; }
        public IReadOnlyList<RaidoMessage>? Messages { get; init; }
    }
}
