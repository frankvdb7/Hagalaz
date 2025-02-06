using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentColorInputMessage : RaidoMessage
    {
        public required int Value { get; init; }
    }
}
