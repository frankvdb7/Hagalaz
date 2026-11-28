using System;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    public class RuinsScriptFactory : IGameObjectScriptFactory
    {
        private readonly IRunecraftingService _runecraftingService;

        public RuinsScriptFactory(IRunecraftingService runecraftingService) => _runecraftingService = runecraftingService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(Ruins);
            foreach (var altar in await _runecraftingService.FindAllAltars())
            {
                yield return (altar.RuinId, scriptType);
            }
        }
    }
}
