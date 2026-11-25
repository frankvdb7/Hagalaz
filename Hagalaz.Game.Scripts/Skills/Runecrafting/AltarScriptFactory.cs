using System;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    public class AltarScriptFactory : IGameObjectScriptFactory
    {
        private readonly IRunecraftingService _runecraftingService;

        public AltarScriptFactory(IRunecraftingService runecraftingService) => _runecraftingService = runecraftingService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(Altar);
            foreach (var altar in await _runecraftingService.FindAllAltars())
            {
                yield return (altar.AltarId, scriptType);
            }
        }
    }
}
