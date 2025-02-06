namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedSlayerDto
    {
        public record SlayerTaskDto
        {
            public required int Id { get; init; }
            public int KillCount { get; init; }
        }
        public SlayerTaskDto? Task { get; init; }
    }
}
