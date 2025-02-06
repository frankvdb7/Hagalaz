using Hagalaz.Game.Abstractions.Model.Items;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class GroundItemClickMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int AbsX { get; init; }
        public required int AbsY { get; init; }
        public required bool ForceRun { get; init; }
        public required GroundItemClickType ClickType { get; init; }
    }
}
