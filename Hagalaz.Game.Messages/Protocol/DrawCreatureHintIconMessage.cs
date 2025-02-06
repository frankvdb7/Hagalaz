using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawCreatureHintIconMessage : RaidoMessage   
    {
        public required int IconIndex { get; init; }
        public required int TargetIndex { get; init; }
        public required bool IsCharacter { get; init; }
        public required int ArrowId { get; init; }
        public required int IconModelId { get; init; }
        public required int FlashSpeed { get; init; }
    }
}
