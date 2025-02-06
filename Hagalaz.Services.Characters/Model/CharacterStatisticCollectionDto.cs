using System.Collections.Generic;

namespace Hagalaz.Services.Characters.Model
{
    public record CharacterStatisticCollectionDto 
    {
        public required string DisplayName { get; init; }
        public IReadOnlyCollection<CharacterStatisticDetailDto> Statistics { get; init; } = default!;
    }
}