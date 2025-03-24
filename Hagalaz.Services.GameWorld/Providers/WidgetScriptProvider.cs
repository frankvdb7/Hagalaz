using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class WidgetScriptProvider : IWidgetScriptProvider, IStartupService
    {
        private readonly ICacheAPI _cacheApi;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDefaultWidgetScriptProvider _defaultWidgetScriptProvider;
        private readonly ILogger<IWidgetScriptProvider> _logger;
        private readonly Dictionary<int, Type> _scripts = new();

        public WidgetScriptProvider(
            ICacheAPI cacheApi, IServiceProvider serviceProvider, IDefaultWidgetScriptProvider defaultWidgetScriptProvider,
            ILogger<IWidgetScriptProvider> logger)
        {
            _cacheApi = cacheApi;
            _serviceProvider = serviceProvider;
            _defaultWidgetScriptProvider = defaultWidgetScriptProvider;
            _logger = logger;
        }

        public Type FindScriptTypeById(int id)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(id);
            return _scripts.TryGetValue(id, out var scriptType) ? scriptType : _defaultWidgetScriptProvider.GetScriptType();
        }

        public int GetInterfacesCount() => _cacheApi.GetFileCount(3);

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var scriptFactories = scope.ServiceProvider.GetServices<IWidgetScriptFactory>();
            foreach (var factory in scriptFactories)
            {
                await foreach (var (scriptId, scriptType) in factory.GetScripts())
                {
                    if (_scripts.TryAdd(scriptId, scriptType))
                    {
                        _logger.LogTrace("Added widget script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate widget script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} widget scripts", _scripts.Count);
        }
    }
}