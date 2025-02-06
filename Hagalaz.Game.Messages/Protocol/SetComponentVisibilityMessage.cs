using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetComponentVisibilityMessage : RaidoMessage
    {
        public required int ComponentId { get; init; }
        public required int ChildId { get; init; }
        public required bool Visible { get; init; }
    }
}
