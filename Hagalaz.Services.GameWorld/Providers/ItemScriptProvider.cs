using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class ItemScriptProvider : IItemScriptProvider, IStartupService
    {
        private readonly IDefaultItemScriptProvider _defaultItemScriptProvider;
        private readonly ILogger<ItemScriptProvider> _logger;
        private readonly Dictionary<int, Type> _scripts;
        private readonly ConcurrentDictionary<int, IItemScript> _scriptInstances;
        private IItemScript? _defaultScript;
        private readonly IServiceScope _serviceScope;

        public ItemScriptProvider(IServiceProvider serviceProvider, IDefaultItemScriptProvider defaultItemScriptProvider, ILogger<ItemScriptProvider> logger)
        {
            ArgumentNullException.ThrowIfNull(defaultItemScriptProvider);
            _defaultItemScriptProvider = defaultItemScriptProvider;
            _logger = logger;
            _scripts = new Dictionary<int, Type>();
            _scriptInstances = new ConcurrentDictionary<int, IItemScript>();
            _serviceScope = serviceProvider.CreateScope();
        }

        public IItemScript FindItemScriptById(int itemId)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(itemId);
            _defaultScript ??= (IItemScript)_serviceScope.ServiceProvider.GetRequiredService(_defaultItemScriptProvider.GetScriptType());
            return _scripts.TryGetValue(itemId, out var scriptType)
                ? _scriptInstances.GetOrAdd(itemId, _ => (IItemScript)_serviceScope.ServiceProvider.GetRequiredService(scriptType))
                : _defaultScript;
        }

        public async Task LoadAsync()
        {
            var itemScriptFactories = _serviceScope.ServiceProvider.GetRequiredService<IEnumerable<IItemScriptFactory>>();
            foreach (var factory in itemScriptFactories)
            {
                await foreach (var (id, type) in factory.GetScripts())
                {
                    if (_scripts.TryAdd(id, type))
                    {
                        _logger.LogTrace("Added item script '{Id}' '{Type}'", id, type.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate item script '{Id}' '{Type}'", id, type.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} item scripts", _scripts.Count);
        }
    }
}