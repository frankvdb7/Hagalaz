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

namespace Hagalaz.Services.GameWorld.Factories
{
    public class NpcScriptMetaDataFactory : INpcScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;
        private readonly IEnumerable<INpcScriptTypeCatalog> _scriptTypeCatalogs;

        public NpcScriptMetaDataFactory(
            IServiceDescriptorProvider serviceDescriptorProvider,
            IEnumerable<INpcScriptTypeCatalog> scriptTypeCatalogs)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider ?? throw new ArgumentNullException(nameof(serviceDescriptorProvider));
            _scriptTypeCatalogs = scriptTypeCatalogs ?? throw new ArgumentNullException(nameof(scriptTypeCatalogs));
        }

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var type = typeof(INpcScript);
            var descriptorScriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => x.ImplementationType)
                .OfType<Type>();
            var catalogScriptTypes = _scriptTypeCatalogs
                .SelectMany(x => x.ScriptTypes)
                .Where(x => x.IsClass && !x.IsAbstract && x.IsAssignableTo(type));
            var scriptTypes = descriptorScriptTypes
                .Concat(catalogScriptTypes)
                .Distinct()
                .Select(x => (ScriptType: x, MetaData: x.GetCustomAttribute<NpcScriptMetaDataAttribute>()));

            foreach (var (scriptType, metaData) in scriptTypes)
            {
                if (scriptType is null)
                {
                    continue;
                }

                if (metaData is null)
                {
                    continue;
                }

                foreach (var npcId in metaData.NpcIds)
                {
                    yield return (npcId, scriptType);
                }
            }
        }
    }
}
