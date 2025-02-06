using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetWeightMessage : RaidoMessage
    {
        public required int Value { get; init; }
    }
}
