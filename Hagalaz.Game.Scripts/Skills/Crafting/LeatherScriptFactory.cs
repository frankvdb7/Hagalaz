using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    public class LeatherScriptFactory : IItemScriptFactory
    {
        private readonly ICraftingService _craftingService;

        public LeatherScriptFactory(ICraftingService craftingService) => _craftingService = craftingService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var leatherType = typeof(Leather);
            var leather = await _craftingService.FindAllLeather();
            foreach (var l in leather)
            {
                yield return (l.ProductId, leatherType);
            }
        }
    }
}
