using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public class FletchingBowScriptFactory : IItemScriptFactory
    {
        private readonly IFletchingService _fletchingService;

        public FletchingBowScriptFactory(IFletchingService fletchingService) => _fletchingService = fletchingService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var bowType = typeof(FletchingBow);
            foreach (var bow in _fletchingService.FindRecipes(FletchingCategory.Bow))
            {
                yield return (bow.ResourceId, bowType);
            }
        }
    }
}
