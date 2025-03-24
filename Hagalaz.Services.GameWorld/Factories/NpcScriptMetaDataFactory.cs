using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class NpcScriptMetaDataFactory : INpcScriptFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public NpcScriptMetaDataFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        // TODO - Once GetSuitableNpcs is removed from INpcScript, we can just retrieve the types instead of creating instances
        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var npcScripts = _serviceProvider.GetServices<INpcScript>();
            foreach (var script in npcScripts)
            {
                var scriptType = script.GetType();
                var metaData = scriptType.GetCustomAttribute<NpcScriptMetaDataAttribute>();
                if (metaData != null)
                {
                    foreach (var npcId in metaData.NpcIds)
                    {
                        yield return (npcId, scriptType);
                    }
                }
                else
                {
                    foreach (var npcId in script.GetSuitableNpcs())
                    {
                        yield return (npcId, scriptType);
                    }
                }
            }
        }
    }
}