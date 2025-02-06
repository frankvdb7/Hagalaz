using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public class FletchingAmmoScriptFactory : IItemScriptFactory
    {
        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var ammoType = typeof(FletchingAmmo);
            foreach (var ammo in Fletching.Ammo)
            {
                yield return (ammo.ResourceID, ammoType);
            }
        }
    }
}