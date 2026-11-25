using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    public class PrimaryIngredientItemScriptFactory : IItemScriptFactory
    {
        private readonly IHerbloreService _herbloreService;

        public PrimaryIngredientItemScriptFactory(IHerbloreService herbloreService) => _herbloreService = herbloreService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var primaryIngredientType = typeof(PrimaryIngredientItemScript);
            foreach (var itemId in _herbloreService.FindPotions(PotionKind.Standard).SelectMany(p => p.PrimaryItemIds).Distinct())
            {
                yield return (itemId, primaryIngredientType);
            }
        }
    }
}
