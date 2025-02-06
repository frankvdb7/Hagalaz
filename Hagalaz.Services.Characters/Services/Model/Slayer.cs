namespace Hagalaz.Services.Characters.Services.Model
{
    public record Slayer
    {
        public record SlayerTask
        {
            public required int Id { get; init; }
            public int KillCount { get; init; }
        }
        public SlayerTask? Task { get; init; }
    }
}
