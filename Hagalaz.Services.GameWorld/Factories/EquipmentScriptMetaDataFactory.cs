using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class EquipmentScriptMetaDataFactory : IEquipmentScriptFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public EquipmentScriptMetaDataFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var equipmentScripts = _serviceProvider.GetServices<IEquipmentScript>();
            foreach (var script in equipmentScripts)
            {
                var scriptType = script.GetType();
                var metaData = scriptType.GetCustomAttribute<EquipmentScriptMetaDataAttribute>();
                if (metaData != null)
                {
                    foreach (var itemId in metaData.ItemIds)
                    {
                        yield return (itemId, scriptType);
                    }
                }
                else
                {
                    foreach (var itemId in script.GetSuitableItems())
                    {
                        yield return (itemId, scriptType);
                    }
                }
            }
        }
    }
}