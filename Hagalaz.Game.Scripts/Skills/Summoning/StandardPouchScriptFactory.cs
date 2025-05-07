using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Summoning
{
    public class StandardPouchScriptFactory : IItemScriptFactory
    {
        private readonly ISummoningService _summoningService;

        public StandardPouchScriptFactory(ISummoningService summoningService) => _summoningService = summoningService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var type = typeof(StandardPouch);
            var definitions = await _summoningService.FindAllDefinitions();
            foreach (var definition in definitions)
            {
                yield return (definition.PouchId, type);
            }
        }
    }
}
