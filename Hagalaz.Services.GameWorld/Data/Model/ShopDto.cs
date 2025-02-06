using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Data.Model
{
    public record ShopDto
    {
        public string Name { get; init; } = default!;
        public int CurrencyId { get; init; }
        public int Capacity { get; init; }
        public bool GeneralStore { get; init; }
        public IEnumerable<ItemDto> MainStock { get; init; } = [];
        public IEnumerable<ItemDto> SampleStock { get; init; } = [];
    }
}