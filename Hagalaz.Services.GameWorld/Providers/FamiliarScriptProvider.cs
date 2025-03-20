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
    public class FamiliarScriptProvider : IFamiliarScriptProvider, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDefaultFamiliarScriptProvider _defaultFamiliarScriptProvider;
        private readonly ILogger<FamiliarScriptProvider> _logger;
        private readonly Dictionary<int, Type> _scripts = new();

        public FamiliarScriptProvider(
            IServiceProvider serviceProvider, IDefaultFamiliarScriptProvider defaultFamiliarScriptProvider, ILogger<FamiliarScriptProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _defaultFamiliarScriptProvider = defaultFamiliarScriptProvider ?? throw new ArgumentNullException(nameof(defaultFamiliarScriptProvider));
            _logger = logger;
        }

        public Type FindFamiliarScriptTypeById(int npcId)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(npcId);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(short.MaxValue, npcId);
            return _scripts.TryGetValue(npcId, out var scriptType) ? scriptType : _defaultFamiliarScriptProvider.GetScriptType();
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var scriptFactories = scope.ServiceProvider.GetServices<IFamiliarScriptFactory>();
            foreach (var factory in scriptFactories)
            {
                await foreach (var (scriptId, scriptType) in factory.GetScripts())
                {
                    if (_scripts.TryAdd(scriptId, scriptType))
                    {
                        _logger.LogTrace("Added familiar script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate familiar script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} familiar scripts", _scripts.Count);
        }
    }
}