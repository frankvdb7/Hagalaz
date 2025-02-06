using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentNumberInputMessage : RaidoMessage
    {
        public required int Value { get; init; }
    }
}
