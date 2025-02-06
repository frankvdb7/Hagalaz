using System;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Messages
{
    public record DehydrateCharacter
    {
        public required uint MasterId { get; init; }
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public required HydratedAppearanceDto Appearance { get; init; }
        public required HydratedDetailsDto Details { get; init; }
        public required HydratedStatisticsDto Statistics { get; init; }
        public required HydratedItemCollectionDto ItemCollection { get; init; }
        public required HydratedFamiliarDto Familiar { get; init; }
        public required HydratedMusicDto Music { get; init; }
        public required HydratedFarmingDto Farming { get; init; }
        public required HydratedSlayerDto Slayer { get; init; }
        public required HydratedNotesDto Notes { get; init; }
        public required HydratedProfileDto Profile { get; init; }
        public required HydratedItemAppearanceCollectionDto ItemAppearanceCollection { get; init; }
        public required HydratedStateDto State { get; init; }
    }
}
