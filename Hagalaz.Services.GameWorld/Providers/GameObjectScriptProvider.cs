using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class GameObjectScriptProvider : IGameObjectScriptProvider, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameObjectScriptProvider> _logger;
        private readonly IDefaultGameObjectScriptProvider _defaultGameObjectScriptProvider;
        private readonly Dictionary<int, Type> _scripts = new();

        public GameObjectScriptProvider(
            IServiceProvider serviceProvider, ILogger<GameObjectScriptProvider> logger, IDefaultGameObjectScriptProvider defaultGameObjectScriptProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _defaultGameObjectScriptProvider = defaultGameObjectScriptProvider;
        }

        public Type GetGameObjectScriptTypeById(int objectId) =>
            _scripts.TryGetValue(objectId, out var scriptType) ? scriptType : _defaultGameObjectScriptProvider.GetScriptType();

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var scriptFactories = scope.ServiceProvider.GetRequiredService<IEnumerable<IGameObjectScriptFactory>>();
            foreach (var factory in scriptFactories)
            {
                await foreach (var (id, type) in factory.GetScripts().WithCancellation(cancellationToken))
                {
                    if (_scripts.TryAdd(id, type))
                    {
                        _logger.LogTrace("Added game object script '{Id}' '{Type}'", id, type.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate game object script '{Id}' '{Type}'", id, type.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} game object scripts", _scripts.Count);
        }
    }
}