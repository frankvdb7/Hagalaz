namespace Hagalaz.Services.Characters.Model
{
    public record CharacterStatisticDetailDto
    {
        public required CharacterStatisticType Type { get; init; }
        public required int Level { get; init; }
        public required double Experience { get; init; }
    }
}
