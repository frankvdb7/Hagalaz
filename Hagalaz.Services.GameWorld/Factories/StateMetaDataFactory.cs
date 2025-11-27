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
            var type = typeof(IState);
            var types = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<StateMetaDataAttribute>()));
            foreach (var (stateType, metaData) in types)
            {
                if (stateType is null || metaData is null)
                {
                    continue;
                }

                yield return (metaData.Id, stateType);
            }
        }
    }
}