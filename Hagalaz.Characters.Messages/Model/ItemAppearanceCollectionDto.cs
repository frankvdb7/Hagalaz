namespace Hagalaz.Characters.Messages.Model
{
    public record ItemAppearanceCollectionDto
    {
        public required IReadOnlyList<ItemAppearanceDto> Appearances { get; init; }
    }
}
