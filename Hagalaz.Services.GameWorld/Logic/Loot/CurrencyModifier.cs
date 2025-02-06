using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    public class CurrencyModifier : IRandomObjectModifier
    {
        private readonly IRatesService _ratesService;

        public CurrencyModifier(IRatesService ratesService)
        {
            _ratesService = ratesService;
        }

        public void Apply(RandomObjectContext context)
        {
            if (context is not LootContext lootContext || lootContext.Loot is not ILootItem item)
            {
                return;
            }

            if (item.Id is not (995 or 6529 or 1815))
            {
                return;
            }

            lootContext.ModifiedProbability *= _ratesService.GetRate<ItemOptions>(i => i.CoinProbabilityRate);
            var countRate = _ratesService.GetRate<ItemOptions>(i => i.CoinCountRate);
            lootContext.ModifiedMinimumCount = (int)(lootContext.ModifiedMinimumCount * countRate);
            lootContext.ModifiedMaximumCount = (int)(lootContext.ModifiedMaximumCount * countRate);
        }
    }
}