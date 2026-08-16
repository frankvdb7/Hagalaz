using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Features.States;
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
        private readonly Dictionary<string, Type> _statesById = new(StringComparer.Ordinal);
        private readonly Dictionary<Type, string> _idsByStateType = new();

        public StateProvider(IServiceProvider serviceProvider, ILogger<StateProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        internal bool TryGetStateType(string id, [NotNullWhen(true)] out Type? stateType) => _statesById.TryGetValue(id, out stateType);

        public bool TryGetStateId(IState state, [NotNullWhen(true)] out string? id) => _idsByStateType.TryGetValue(state.GetType(), out id);

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var stateFactories = scope.ServiceProvider.GetRequiredService<IEnumerable<IStateFactory>>();
            foreach (var factory in stateFactories)
            {
                await foreach (var (id, type) in factory.GetStates().WithCancellation(cancellationToken))
                {
                    if (!typeof(IPersistentState).IsAssignableFrom(type))
                    {
                        _logger.LogDebug("Skipping non-persistent state '{Type}' from state registry.", type.FullName);
                        continue;
                    }

                    if (type.GetCustomAttribute<StateMetaDataAttribute>() is null)
                    {
                        throw new InvalidOperationException($"Persistent state '{type.FullName}' must declare StateMetaDataAttribute with a stable ID.");
                    }

                    if (!_statesById.TryAdd(id, type))
                    {
                        throw new InvalidOperationException($"Duplicate state ID '{id}' was registered for '{type.FullName}'.");
                    }

                    if (!_idsByStateType.TryAdd(type, id))
                    {
                        throw new InvalidOperationException($"State type '{type.FullName}' has multiple registered IDs.");
                    }

                    _logger.LogTrace("Added state '{Id}' '{Type}'", id, type.FullName);
                }
            }

            _logger.LogInformation("Loaded {Count} states", _statesById.Count);
        }
    }
}
