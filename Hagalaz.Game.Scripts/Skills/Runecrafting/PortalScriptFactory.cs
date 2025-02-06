using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    public class PortalScriptFactory : IGameObjectScriptFactory
    {
        private readonly IRunecraftingService _runecraftingService;

        public PortalScriptFactory(IRunecraftingService runecraftingService) => _runecraftingService = runecraftingService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(Portal);
            foreach (var altar in await _runecraftingService.FindAllAltars())
            {
                yield return (altar.PortalId, scriptType);
            }
        }
    }
}
