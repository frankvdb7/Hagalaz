using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public class FletchingBowScriptFactory : IItemScriptFactory
    {
        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var bowType = typeof(FletchingBow);
            foreach (var bow in Fletching.Bows)
            {
                yield return (bow.ResourceID, bowType);
            }
        }
    }
}