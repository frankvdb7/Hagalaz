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
    public class StateProvider : IStateProvider, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StateProvider> _logger;
        private readonly Dictionary<string, Type> _states = new();

        public StateProvider(IServiceProvider serviceProvider, ILogger<StateProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Type GetStateById(string id) => _states[id];

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var scriptFactories = scope.ServiceProvider.GetRequiredService<IEnumerable<IStateFactory>>();
            foreach (var factory in scriptFactories)
            {
                await foreach (var (id, type) in factory.GetStates().WithCancellation(cancellationToken))
                {
                    if (_states.TryAdd(id, type))
                    {
                        _logger.LogTrace("Added state '{Id}' '{Type}'", id, type.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate state '{Id}' '{Type}'", id, type.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} states", _states.Count);
        }
    }
}