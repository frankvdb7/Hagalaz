namespace Hagalaz.Game.Abstractions.Services.Model
{
    public record RockDto
    {
        public required int RockId { get; init; }
        public required int ExhaustRockId { get; init; }
        public required int OreId { get; init; }
    }
}
