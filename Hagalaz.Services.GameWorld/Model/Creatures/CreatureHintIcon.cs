using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    public class CreatureHintIcon : IHintIcon
    {
        public int Index { get; set; }
        public int TargetIndex { get; init; }
        public bool IsCharacter { get; init; }
        public int ModelId { get; init; }
        public int FlashSpeed { get; init; }
        public int ArrowId { get; init; }

        public RaidoMessage ToMessage() => new DrawCreatureHintIconMessage
        {
            IconIndex = Index,
            IconModelId = ModelId,
            IsCharacter = IsCharacter,
            ArrowId = ArrowId,
            TargetIndex = TargetIndex,
            FlashSpeed = FlashSpeed,
        };
    }
}