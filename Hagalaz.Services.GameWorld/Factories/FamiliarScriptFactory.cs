using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class FamiliarScriptFactory : IFamiliarScriptFactory
    {
        private readonly IEnumerable<INpcScriptFactory> _npcScriptFactories;

        public FamiliarScriptFactory(IEnumerable<INpcScriptFactory> npcScriptFactories) => _npcScriptFactories = npcScriptFactories;

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts()
        {
            var familiarScriptType = typeof(IFamiliarScript);
            foreach (var factory in _npcScriptFactories)
            {
                await foreach (var (npcId, scriptType) in factory.GetScripts())
                {
                    if (familiarScriptType.IsAssignableFrom(scriptType))
                    {
                        yield return (npcId, scriptType);
                    }
                }
            }
        }
    }
}