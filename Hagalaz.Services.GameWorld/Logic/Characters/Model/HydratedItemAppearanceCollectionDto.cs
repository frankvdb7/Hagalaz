using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedItemAppearanceCollectionDto
    {
        public IReadOnlyList<HydratedItemAppearanceDto> Appearances { get; init; } = default!;
    }
}
