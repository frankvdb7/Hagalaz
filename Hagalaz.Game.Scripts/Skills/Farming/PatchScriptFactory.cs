using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Farming
{
    public class PatchScriptFactory : IGameObjectScriptFactory
    {
        private readonly IFarmingService _farmingService;

        public PatchScriptFactory(IFarmingService farmingService) => _farmingService = farmingService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(Patch);
            var patches = await _farmingService.FindAllPatches();
            foreach (var patch in patches)
            {
                yield return (patch.ObjectID, scriptType);
            }
        }
    }
}
