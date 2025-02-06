using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    public class RiftScriptFactory : IGameObjectScriptFactory
    {
        private readonly IRunecraftingService _runecraftingService;

        public RiftScriptFactory(IRunecraftingService runecraftingService) => _runecraftingService = runecraftingService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(Rift);
            foreach (var altar in await _runecraftingService.FindAllAltars())
            {
                yield return (altar.RiftId, scriptType);
            }
        }
    }
}
