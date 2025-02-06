using System.Collections.Generic;

namespace Hagalaz.Services.Characters.Services.Model
{
    public record Farming
    {
        public record Patch
        {
            public int Id { get; init; }
            public int SeedId { get; init; }
            public int CurrentCycle { get; init; }
            public int CurrentCycleTicks { get; init; }
            public int ProductCount { get; init; }
            public int Condition { get; init; }
        }
        public IReadOnlyList<Patch> Patches { get; init; } = new List<Patch>();
    }
}
