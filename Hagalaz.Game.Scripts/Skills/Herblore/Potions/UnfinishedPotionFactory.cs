using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    public class UnfinishedPotionFactory : IItemScriptFactory
    {
        private readonly IHerbloreService _herbloreService;

        public UnfinishedPotionFactory(IHerbloreService herbloreService)
        {
            _herbloreService = herbloreService;
        }

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var type = typeof(UnfinishedPotion);
            var allPotions = await _herbloreService.FindAllPotions();
            foreach (var potion in allPotions.DistinctBy(p => p.UnfinishedPotionId))
            {
                yield return (potion.UnfinishedPotionId, type);
            }
        }
    }
}
