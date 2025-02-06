using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    public class RawOreScriptFactory : IItemScriptFactory
    {
        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var rawOreType = typeof(RawOre);
            foreach (var t in Smithing.Bars)
            {
                foreach (var t1 in t.SmeltDefinition.RequiredOres.DistinctBy(t1 => t1.Id))
                {
                    yield return (t1.Id, rawOreType);
                }
            }
        }
    }
}