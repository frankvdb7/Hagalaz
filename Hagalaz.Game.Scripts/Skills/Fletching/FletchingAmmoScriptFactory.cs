using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public class FletchingAmmoScriptFactory : IItemScriptFactory
    {
        private readonly IFletchingService _fletchingService;

        public FletchingAmmoScriptFactory(IFletchingService fletchingService) => _fletchingService = fletchingService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var ammoType = typeof(FletchingAmmo);
            foreach (var ammo in _fletchingService.FindRecipes(FletchingCategory.Ammo))
            {
                yield return (ammo.ResourceId, ammoType);
            }
        }
    }
}
