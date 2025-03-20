using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class AreaScriptProvider : IAreaScriptProvider, IStartupService
    {
        private readonly IDefaultAreaScriptProvider _defaultAreaScriptProvider;
        private readonly ILogger<AreaScriptProvider> _logger;
        private readonly Dictionary<int, Type> _scripts;
        private readonly IServiceScope _serviceScope;

        public AreaScriptProvider(IServiceProvider serviceProvider, IDefaultAreaScriptProvider defaultAreaScriptProvider, ILogger<AreaScriptProvider> logger)
        {
            _defaultAreaScriptProvider = defaultAreaScriptProvider ?? throw new ArgumentNullException(nameof(defaultAreaScriptProvider));
            _logger = logger;
            _serviceScope = serviceProvider.CreateScope();
            _scripts = new Dictionary<int, Type>();
        }

        public IAreaScript FindAreaScriptById(int areaId)
        {
            if (_scripts.TryGetValue(areaId, out var scriptType))
            {
                return (IAreaScript)_serviceScope.ServiceProvider.GetRequiredService(scriptType);
            }
            return (IAreaScript)_serviceScope.ServiceProvider.GetRequiredService(_defaultAreaScriptProvider.GetScriptType());
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            var factories = _serviceScope.ServiceProvider.GetRequiredService<IEnumerable<IAreaScriptFactory>>();
            foreach (var factory in factories)
            {
                await foreach (var (id, type) in factory.GetScripts())
                {
                    if (_scripts.TryAdd(id, type))
                    {
                        _logger.LogTrace("Added area script '{Id}' '{Type}'", id, type.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate area script '{Id}' '{Type}'", id, type.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} area scripts", _scripts.Count);
        }
    }
}