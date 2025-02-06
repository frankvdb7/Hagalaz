using System;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.States
{
    public class CharacterDehydrationState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid? RequestId { get; set; }
        public Guid? UpdateCharacterRequestId { get; set; }
        public Uri? ResponseAddress { get; set; }
        public HydratedAppearanceDto? Appearance { get; set; }
        public HydratedDetailsDto? Details { get; set; }
        public HydratedStatisticsDto? Statistics { get; set; }
        public HydratedItemCollectionDto? ItemCollection { get; set; }
        public HydratedFamiliarDto? Familiar { get; set; }
        public HydratedNotesDto? Notes { get; set; }
        public HydratedProfileDto? Profile { get; set; }
        public HydratedItemAppearanceCollectionDto? ItemAppearanceCollection { get; set; }
        public HydratedMusicDto? Music { get; set; }
        public HydratedFarmingDto? Farming { get; set; }
        public HydratedSlayerDto? Slayer { get; set; }
        public HydratedStateDto? State { get; set; }
        public string CurrentState { get; set; } = default!;

        public uint MasterId { get; set; }
    }
}
