using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class NpcScriptMetaDataFactory : INpcScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;
        private readonly ILogger<NpcScriptMetaDataFactory> _logger;

        public NpcScriptMetaDataFactory(
            IServiceDescriptorProvider serviceDescriptorProvider,
            ILogger<NpcScriptMetaDataFactory> logger)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider ?? throw new ArgumentNullException(nameof(serviceDescriptorProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var type = typeof(INpcScript);
            var discoveredTypes = new HashSet<Type>();
            var scriptTypes = new List<Type>();

            foreach (var assembly in _serviceDescriptorProvider.GetServiceDescriptors()
                         .Where(descriptor => descriptor.ServiceType == typeof(Assembly))
                         .Select(descriptor => descriptor.ImplementationInstance as Assembly)
                         .OfType<Assembly>()
                         .Distinct()
                         .OrderBy(GetAssemblySortKey, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var candidate in GetLoadableTypes(assembly)
                             .Where(candidate => candidate.IsClass && !candidate.IsAbstract && candidate.IsAssignableTo(type))
                             .OrderBy(candidate => candidate.FullName, StringComparer.Ordinal))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (discoveredTypes.Add(candidate))
                    {
                        scriptTypes.Add(candidate);
                    }
                }
            }

            foreach (var scriptType in scriptTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var metaData = scriptType.GetCustomAttribute<NpcScriptMetaDataAttribute>();

                if (metaData is null)
                {
                    continue;
                }

                foreach (var npcId in metaData.NpcIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return (npcId, scriptType);
                }
            }
        }

        private static string GetAssemblySortKey(Assembly assembly)
        {
            try
            {
                return assembly.FullName ?? assembly.GetType().FullName ?? string.Empty;
            }
            catch (NotImplementedException)
            {
                return assembly.GetType().FullName ?? string.Empty;
            }
        }

        private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                _logger.LogWarning(exception, "A plugin assembly was only partially loadable; usable types will still be discovered.");
                return exception.Types.OfType<Type>();
            }
        }
    }
}
