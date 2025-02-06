namespace Hagalaz.Game.Abstractions.Services.Model
{
    public record MusicDto
    {
        public int Id { get; init; }
        public required string Name { get; init; }
        public required string Hint { get; init; }
    }
}