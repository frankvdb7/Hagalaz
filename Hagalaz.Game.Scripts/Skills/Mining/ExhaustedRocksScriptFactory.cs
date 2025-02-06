using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Mining
{
    public class ExhaustedRocksScriptFactory : IGameObjectScriptFactory
    {
        private readonly IMiningService _miningService;

        public ExhaustedRocksScriptFactory(IMiningService miningService) => _miningService = miningService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(ExhaustedRocks);
            var allRocks = await _miningService.FindAllRocks();
            foreach (var rock in allRocks.DistinctBy(r => r.ExhaustRockId))
            {
                yield return (rock.ExhaustRockId, scriptType);
            }
        }
    }
}
