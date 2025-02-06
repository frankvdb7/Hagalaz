using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    public class PrimaryIngredientItemScriptFactory : IItemScriptFactory
    {
        private readonly IHerbloreService _herbloreService;

        public PrimaryIngredientItemScriptFactory(IHerbloreService herbloreService)
        {
            _herbloreService = herbloreService;
        }

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var primaryIngredientType = typeof(PrimaryIngredientItemScript);
            var allPotions = await _herbloreService.FindAllPotions();
            foreach (var itemId in allPotions.SelectMany(p => p.PrimaryItemIds).Distinct())
            {
                yield return (itemId, primaryIngredientType);
            }
        }
    }
}
