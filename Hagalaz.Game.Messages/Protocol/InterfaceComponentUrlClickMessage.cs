using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentUrlClickMessage : RaidoMessage
    {
        public required string Type { get; init; } = default!;
        public required string Title { get; init; } = default!;
        public required string Unknown { get; init; } = default!; 
        public required bool SomeBool { get; init; }
        public required bool SomeBool2 { get; init; }
        public required int StringLength { get; init; }
    }
}
