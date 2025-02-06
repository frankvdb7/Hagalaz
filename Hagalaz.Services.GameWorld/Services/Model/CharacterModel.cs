using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    public record CharacterModel
    {
        public HydratedAppearanceDto Appearance { get; init; } = default!;
        public HydratedDetailsDto Details { get; init; } = default!;
        public HydratedStatisticsDto Statistics { get; init; } = default!;
        public HydratedItemCollectionDto ItemCollection { get; init; } = default!;
        public HydratedFamiliarDto? Familiar { get; init; }
        public HydratedClaims Claims { get; init; } = default!;
        public HydratedMusicDto Music { get; init; } = default!;
        public HydratedFarmingDto Farming { get; init; } = default!;
        public HydratedSlayerDto Slayer { get; init; } = default!;
        public HydratedNotesDto Notes { get; init; } = default!;
        public HydratedProfileDto Profile { get; init; } = default!;
        public HydratedItemAppearanceCollectionDto ItemAppearanceCollection { get; init; } = default!;
        public HydratedStateDto State { get; init; } = default!;
    }
}
