using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class WidgetScriptMetaDataFactory : IWidgetScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;

        public WidgetScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider;
        }

        public async IAsyncEnumerable<(int widgetId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var type = typeof(IWidgetScript);
            var scriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<WidgetScriptMetaData>()));

            foreach (var (scriptType, metaData) in scriptTypes)
            {
                if (scriptType is null || metaData is null)
                {
                    continue;
                }

                foreach (var widgetId in metaData.WidgetIds)
                {
                    yield return (widgetId, scriptType);
                }
            }
        }
    }
}