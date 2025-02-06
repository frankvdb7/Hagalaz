namespace Hagalaz.Characters.Messages.Model
{
    public record FarmingDto
    {
        public record PatchDto
        {
            public int Id { get; init; }
            public int SeedId { get; init; }
            public int CurrentCycle { get; init; }
            public int CurrentCycleTicks { get; init; }
            public int ProductCount { get; init; }
            public int Condition { get; init; }
        }
        public IReadOnlyList<PatchDto> Patches { get; init; } = new List<PatchDto>();
    }
}
