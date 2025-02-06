using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class AreaScriptMetaDataFactory : IAreaScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;

        public AreaScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider;
        }
        public async IAsyncEnumerable<(int areaId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var type = typeof(IAreaScript);
            var scriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<AreaScriptMetaDataAttribute>()));

            foreach (var (scriptType, metaData) in scriptTypes)
            {
                if (scriptType is null || metaData is null)
                {
                    continue;
                }

                foreach (var areaId in metaData.AreaIds)
                {
                    yield return (areaId, scriptType);
                }
            }
        }
    }
}