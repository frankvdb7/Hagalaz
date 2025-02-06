using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedFarmingDto
    {
        public record PatchDto
        {
            public int Id { get; init; }
            public int SeedId { get; init; }
            public int CurrentCycle { get; init; }
            public int CurrentCycleTicks { get; init; }
            public int ProductCount { get; init; }
            public PatchCondition Condition { get; init; }
        }
        public IReadOnlyList<PatchDto> Patches { get; init; } = new List<PatchDto>();
    }
}
