using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class StateMetaDataFactory : IStateFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;

        public StateMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider) => _serviceDescriptorProvider = serviceDescriptorProvider;

        public async IAsyncEnumerable<(string stateId, Type scriptType)> GetStates()
        {
            await Task.CompletedTask;
            var type = typeof(IPersistentState);
            var stateTypes = new HashSet<Type>();
            foreach (var descriptor in _serviceDescriptorProvider.GetServiceDescriptors().Where(x => x.ServiceType.IsAssignableTo(type)))
            {
                if (descriptor.ImplementationType is not { } stateType)
                {
                    throw new InvalidOperationException($"Persistent state registration '{descriptor.ServiceType.FullName}' must expose an implementation type with StateMetaDataAttribute.");
                }

                if (!stateTypes.Add(stateType))
                {
                    continue;
                }

                var metaData = stateType.GetCustomAttribute<StateMetaDataAttribute>();
                if (metaData is null)
                {
                    throw new InvalidOperationException($"Persistent state '{stateType.FullName}' must declare StateMetaDataAttribute with a stable ID.");
                }

                yield return (metaData.Id, stateType);
            }
        }
    }
}
