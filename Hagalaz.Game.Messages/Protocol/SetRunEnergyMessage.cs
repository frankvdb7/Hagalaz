using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetRunEnergyMessage : RaidoMessage
    {
        public required int Energy { get; init; }
    }
}
