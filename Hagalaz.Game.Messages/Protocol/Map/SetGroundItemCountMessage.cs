using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class SetGroundItemCountMessage : RaidoMessage
    {
        public int ItemId { get; init; }
        public int OldCount { get; init; }
        public int NewCount { get; init; }
        public int PartLocalX { get; init; }
        public int PartLocalY { get; init; }
    }
}
