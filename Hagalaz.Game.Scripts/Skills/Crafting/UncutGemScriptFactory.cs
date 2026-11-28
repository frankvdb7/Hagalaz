using System;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    public class UncutGemScriptFactory : IItemScriptFactory
    {
        private readonly ICraftingService _craftingService;

        public UncutGemScriptFactory(ICraftingService craftingService) => _craftingService = craftingService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var gemType = typeof(UncutGem);
            var gems = await _craftingService.FindAllGems();
            foreach (var gem in gems)
            {
                yield return (gem.UncutGemID, gemType);
            }
        }
    }
}
