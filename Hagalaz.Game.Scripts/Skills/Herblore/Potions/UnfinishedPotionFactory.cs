using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    public class UnfinishedPotionFactory : IItemScriptFactory
    {
        private readonly IHerbloreService _herbloreService;

        public UnfinishedPotionFactory(IHerbloreService herbloreService) => _herbloreService = herbloreService;

        /// <summary>
        ///     Only recipes made from a primary ingredient have a real unfinished potion; the others (e.g. extreme potions) are made from a
        ///     finished potion that has its own script.
        /// </summary>
        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var type = typeof(UnfinishedPotion);
            foreach (var potion in _herbloreService.FindPotions(PotionKind.Standard).Where(p => p.PrimaryItemIds.Length > 0).DistinctBy(p => p.UnfinishedPotionId))
            {
                yield return (potion.UnfinishedPotionId, type);
            }
        }
    }
}
