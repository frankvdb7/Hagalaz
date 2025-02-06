using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.GameObjects
{
    public class LodestoneScriptFactory : IGameObjectScriptFactory
    {
        private readonly ILodestoneService _lodestoneService;

        public LodestoneScriptFactory(ILodestoneService lodestoneService) => _lodestoneService = lodestoneService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var type = typeof(Lodestone);
            foreach (var stone in await _lodestoneService.FindAll())
            {
                yield return (stone.GameObjectId, type);
            }
        }
    }
}
