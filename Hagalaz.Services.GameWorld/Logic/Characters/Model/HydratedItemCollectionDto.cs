using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedItemCollectionDto
    {
        public IReadOnlyList<HydratedItemDto> Bank { get; init; } = default!;
        public IReadOnlyList<HydratedItemDto> Inventory { get; init; } = default!;
        public IReadOnlyList<HydratedItemDto> FamiliarInventory { get; init; } = default!;
        public IReadOnlyList<HydratedItemDto> Equipment { get; init; } = default!;
        public IReadOnlyList<HydratedItemDto> Rewards { get; init; } = default!;
        public IReadOnlyList<HydratedItemDto> MoneyPouch { get; init; } = default!;
    }
}
