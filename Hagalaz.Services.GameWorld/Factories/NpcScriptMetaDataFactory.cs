using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class NpcScriptMetaDataFactory : INpcScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;
        private readonly IServiceProvider _serviceProvider;

        public NpcScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider, IServiceProvider serviceProvider)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider;
            _serviceProvider = serviceProvider;
        }

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var type = typeof(INpcScript);
            var scriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<NpcScriptMetaDataAttribute>()));

            foreach (var (scriptType, metaData) in scriptTypes)
            {
                if (scriptType is null)
                {
                    continue;
                }

                if (metaData is not null)
                {
                    foreach (var npcId in metaData.NpcIds)
                    {
                        yield return (npcId, scriptType);
                    }
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var script = (INpcScript)_serviceProvider.GetRequiredService(scriptType);
                    foreach (var npcId in script.GetSuitableNpcs())
                    {
                        yield return (npcId, scriptType);
                    }
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }
    }
}
