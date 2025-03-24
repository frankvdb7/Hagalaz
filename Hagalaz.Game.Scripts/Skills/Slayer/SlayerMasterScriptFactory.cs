using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    public class SlayerMasterScriptFactory : INpcScriptFactory
    {
        private readonly ISlayerService _slayerService;

        public SlayerMasterScriptFactory(ISlayerService slayerService) => _slayerService = slayerService;

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var scriptType = typeof(SlayerMaster);
            var masterTables = await _slayerService.FindAllSlayerMasterTables(cancellationToken);
            foreach (var table in masterTables)
            {
                yield return (table.Id, scriptType);
            }
        }
    }
}
