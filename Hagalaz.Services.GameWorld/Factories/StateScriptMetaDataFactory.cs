using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class StateScriptMetaDataFactory : IStateScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;

        public StateScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider) => _serviceDescriptorProvider = serviceDescriptorProvider;

        public async IAsyncEnumerable<(Type stateType, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var type = typeof(IStateScript);
            var scriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<StateScriptMetaData>()));

            foreach (var (scriptType, metaData) in scriptTypes)
            {
                if (scriptType is null || metaData is null)
                {
                    continue;
                }

                yield return (metaData.StateType, scriptType);
            }
        }
    }
}