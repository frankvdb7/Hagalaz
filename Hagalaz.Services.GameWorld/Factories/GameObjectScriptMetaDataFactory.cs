using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class GameObjectScriptMetaDataFactory : IGameObjectScriptFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GameObjectScriptMetaDataFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var objectScripts = _serviceProvider.GetServices<IGameObjectScript>();
            foreach (var script in objectScripts)
            {
                var scriptType = script.GetType();
                var metaData = scriptType.GetCustomAttribute<GameObjectScriptMetaDataAttribute>();
                if (metaData != null)
                {
                    foreach (var objectId in metaData.ObjectIds)
                    {
                        yield return (objectId, scriptType);
                    }
                }
            }
        }
    }
}