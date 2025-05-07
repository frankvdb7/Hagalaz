using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public class FletchingTipScriptFactory : IItemScriptFactory
    {
        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var tipType = typeof(FletchingTips);
            foreach (var tip in FletchingSkillService.Tips)
            {
                yield return (tip.ResourceID, tipType);
            }
        }
    }
}