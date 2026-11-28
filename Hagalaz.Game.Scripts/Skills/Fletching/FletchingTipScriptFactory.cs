using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public class FletchingTipScriptFactory : IItemScriptFactory
    {
        private readonly IFletchingService _fletchingService;

        public FletchingTipScriptFactory(IFletchingService fletchingService) => _fletchingService = fletchingService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var tipType = typeof(FletchingTips);
            foreach (var tip in _fletchingService.FindRecipes(FletchingCategory.Tips))
            {
                yield return (tip.ResourceId, tipType);
            }
        }
    }
}
