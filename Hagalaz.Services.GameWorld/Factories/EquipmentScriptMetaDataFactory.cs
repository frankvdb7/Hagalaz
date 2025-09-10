using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class EquipmentScriptMetaDataFactory : IEquipmentScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;

        public EquipmentScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider;
        }

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var type = typeof(IEquipmentScript);
            var scriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<EquipmentScriptMetaDataAttribute>()));

            foreach (var (scriptType, metaData) in scriptTypes)
            {
                if (scriptType is null || metaData is null)
                {
                    continue;
                }

                foreach (var itemId in metaData.ItemIds)
                {
                    yield return (itemId, scriptType);
                }
            }
        }
    }
}