namespace Hagalaz.Game.Abstractions.Services.Model
{
    public record EnchantingSpellProductDto
    {
        public required int ResourceId { get; init; }
        public required int ButtonId { get; init; }
        public required int ProductId { get; init; }
    }
}
