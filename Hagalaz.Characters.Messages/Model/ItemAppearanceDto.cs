namespace Hagalaz.Characters.Messages.Model
{
    public record ItemAppearanceDto
    {
        public int Id { get; init; }
        public int[] MaleModels { get; init; } = default!;
        public int[] FemaleModels { get; init; } = default!;
        public int[] ModelColors { get; init; } = default!;
        public int[] TextureColors { get; init; } = default!;
    }
}
