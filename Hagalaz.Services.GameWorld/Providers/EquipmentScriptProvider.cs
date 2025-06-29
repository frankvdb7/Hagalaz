using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class EquipmentScriptProvider : IEquipmentScriptProvider, IStartupService
    {
        private readonly IDefaultEquipmentScriptProvider _defaultEquipmentScriptProvider;
        private readonly ILogger<EquipmentScriptProvider> _logger;
        private readonly Dictionary<int, Type> _scripts;
        private readonly ConcurrentDictionary<int, IEquipmentScript> _scriptInstances;
        private IEquipmentScript? _defaultScript;
        private readonly IServiceScope _serviceScope;

        public EquipmentScriptProvider(
            IServiceProvider serviceProvider, IDefaultEquipmentScriptProvider defaultEquipmentScriptProvider, ILogger<EquipmentScriptProvider> logger)
        {
            ArgumentNullException.ThrowIfNull(defaultEquipmentScriptProvider);
            _defaultEquipmentScriptProvider = defaultEquipmentScriptProvider;
            _logger = logger;
            _serviceScope = serviceProvider.CreateScope();
            _scripts = new Dictionary<int, Type>();
            _scriptInstances = new ConcurrentDictionary<int, IEquipmentScript>();
        }

        public IEquipmentScript FindEquipmentScriptById(int itemId)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(itemId);
            _defaultScript ??= (IEquipmentScript)_serviceScope.ServiceProvider.GetRequiredService(_defaultEquipmentScriptProvider.GetScriptType());
            return _scripts.TryGetValue(itemId, out var scriptType)
                ? _scriptInstances.GetOrAdd(itemId, _ => (IEquipmentScript)_serviceScope.ServiceProvider.GetRequiredService(scriptType))
                : _defaultScript;
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            var factories = _serviceScope.ServiceProvider.GetRequiredService<IEnumerable<IEquipmentScriptFactory>>();
            foreach (var factory in factories)
            {
                await foreach (var (id, type) in factory.GetScripts(cancellationToken))
                {
                    if (_scripts.TryAdd(id, type))
                    {
                        _logger.LogTrace("Added equipment script '{Id}' '{Type}'", id, type.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate equipment script '{Id}' '{Type}'", id, type.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} equipment scripts", _scripts.Count);
        }
    }
}