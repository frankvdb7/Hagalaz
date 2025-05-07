using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    public class GrimyHerbItemScriptFactory : IItemScriptFactory
    {
        private readonly IHerbloreService _herbloreService;

        public GrimyHerbItemScriptFactory(IHerbloreService herbloreService) => _herbloreService = herbloreService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var herbType = typeof(GrimyHerbItemScript);
            foreach (var herb in await _herbloreService.FindAllHerbs())
            {
                yield return (herb.GrimyHerbId, herbType);
            }
        }
    }
}
