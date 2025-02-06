using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentUseOnCharacterMessage : RaidoMessage
    {
        public required int InterfaceId { get; init; }
        public required int ComponentId { get; init; }
        public required int ExtraData1 { get; init; }
        public required int ExtraData2 { get; init; }

        public required int Index { get; init; }
        public required bool ForceRun { get; init; }
    }
}
