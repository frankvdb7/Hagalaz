namespace Hagalaz.Characters.Messages.Model
{
    public record StateDto
    {
        public record StateExDto
        {
            public required string Id { get; init; }
            public required int TicksLeft { get; init; }
        }

        public required IReadOnlyList<StateExDto> StatesEx { get; init; }
    }
}
