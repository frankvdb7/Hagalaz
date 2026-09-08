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

        public NpcScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider;
        }

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var type = typeof(INpcScript);
            var descriptorScriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => x.ImplementationType)
                .OfType<Type>();
            var loadedScriptTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(x => x.IsClass && !x.IsAbstract && x.IsAssignableTo(type));
            var scriptTypes = descriptorScriptTypes
                .Concat(loadedScriptTypes)
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

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.OfType<Type>();
            }
        }
    }
}
