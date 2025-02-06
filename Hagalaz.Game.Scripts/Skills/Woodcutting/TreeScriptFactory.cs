using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Woodcutting
{
    public class TreeScriptFactory : IGameObjectScriptFactory
    {
        private readonly IWoodcuttingService _woodcuttingService;

        public TreeScriptFactory(IWoodcuttingService woodcuttingService) => _woodcuttingService = woodcuttingService;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            var scriptType = typeof(Tree);
            var trees = await _woodcuttingService.FindAllTrees();
            foreach (var tree in trees)
            {
                yield return (tree.Id, scriptType);
            }
        }
    }
}
