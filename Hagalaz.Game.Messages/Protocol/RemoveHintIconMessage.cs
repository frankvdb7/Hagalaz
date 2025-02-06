using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class RemoveHintIconMessage : RaidoMessage
    {
        public required int IconIndex { get; init; }
    }
}
