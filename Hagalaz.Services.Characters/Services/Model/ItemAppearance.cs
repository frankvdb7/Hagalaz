namespace Hagalaz.Services.Characters.Services.Model
{
    public record ItemAppearance
    {
        public int Id { get; init; }
        public int[] MaleModels { get; init; } = default!;
        public int[] FemaleModels { get; init; } = default!;
        public int[] ModelColors { get; init; } = default!;
        public int[] TextureColors { get; init; } = default!;
    }
}
