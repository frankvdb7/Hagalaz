using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    public class SecondaryIngredientItemScriptFactory : IItemScriptFactory
    {
        private readonly IHerbloreService _herbloreService;

        public SecondaryIngredientItemScriptFactory(IHerbloreService herbloreService) => _herbloreService = herbloreService;

        /// <summary>
        ///     Herbs that are also a primary ingredient (e.g. avantoe for extreme attack) keep their <see cref="PrimaryIngredientItemScript" />;
        ///     the potion they are added to resolves that recipe instead.
        /// </summary>
        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var secondaryIngredientType = typeof(SecondaryIngredientItemScript);
            var potions = _herbloreService.FindPotions(PotionKind.Standard);
            var primaryItemIds = potions.SelectMany(p => p.PrimaryItemIds).ToHashSet();
            foreach (var itemId in potions.SelectMany(p => p.SecondaryItemIds).Distinct().Where(id => !primaryItemIds.Contains(id)))
            {
                yield return (itemId, secondaryIngredientType);
            }
        }
    }
}
