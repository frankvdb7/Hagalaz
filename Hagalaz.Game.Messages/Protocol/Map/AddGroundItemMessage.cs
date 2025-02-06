using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class AddGroundItemMessage : RaidoMessage
    {
        public int ItemId { get; init; }
        public int Count { get; init; }
        public int PartLocalX { get; init; }
        public int PartLocalY { get; init; }
    }
}
