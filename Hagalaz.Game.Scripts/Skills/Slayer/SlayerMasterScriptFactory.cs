using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    public class SlayerMasterScriptFactory : INpcScriptFactory
    {
        private readonly ISlayerService _slayerService;

        public SlayerMasterScriptFactory(ISlayerService slayerService) => _slayerService = slayerService;

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(SlayerMaster);
            var masterTables = await _slayerService.FindAllSlayerMasterTables();
            foreach (var table in masterTables)
            {
                yield return (table.NpcId, scriptType);
            }
        }
    }
}
