using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    public class SecondaryIngredientItemScriptFactory : IItemScriptFactory
    {
        private readonly IHerbloreService _herbloreService;

        public SecondaryIngredientItemScriptFactory(IHerbloreService herbloreService) => _herbloreService = herbloreService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var secondaryIngredientType = typeof(SecondaryIngredientItemScript);
            var allPotions = await _herbloreService.FindAllPotions();
            foreach (var itemId in allPotions.SelectMany(p => p.SecondaryItemIds).Distinct())
            {
                yield return (itemId, secondaryIngredientType);
            }
        }
    }
}