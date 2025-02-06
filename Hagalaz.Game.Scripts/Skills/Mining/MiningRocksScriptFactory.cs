using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Mining
{
    public class MiningRocksScriptFactory : IGameObjectScriptFactory
    {
        private readonly IMiningService _miningService;

        public MiningRocksScriptFactory(IMiningService miningService) => _miningService = miningService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var type = typeof(MiningRocks);
            foreach (var rock in await _miningService.FindAllRocks())
            {
                yield return (rock.RockId, type);
            }
        }
    }
}
