using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class ItemScriptMetaDataFactory : IItemScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;

        public ItemScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider;
        }

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var type = typeof(IItemScript);
            var scriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<ItemScriptMetaDataAttribute>()));

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