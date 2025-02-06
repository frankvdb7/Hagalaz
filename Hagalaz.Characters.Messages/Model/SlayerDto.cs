namespace Hagalaz.Characters.Messages.Model
{
    public record SlayerDto
    {
        public record SlayerTaskDto
        {
            public required int Id { get; init; }
            public int KillCount { get; init; }
        }
        public SlayerTaskDto? Task { get; init; }
    }
}
