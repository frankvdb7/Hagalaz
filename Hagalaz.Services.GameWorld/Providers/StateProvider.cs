using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public bool TryCreateState(string id, [NotNullWhen(true)] out IState? state)
        {
            state = null;
            if (!_statesById.TryGetValue(id, out var stateType))
            {
                return false;
            }

            state = (IState)_serviceProvider.GetRequiredService(stateType);
            return true;
        }

        public bool TryGetStateId(IState state, [NotNullWhen(true)] out string? id) => _idsByStateType.TryGetValue(state.GetType(), out id);

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var stateFactories = scope.ServiceProvider.GetRequiredService<IEnumerable<IStateFactory>>();
            foreach (var factory in stateFactories)
            {
                await foreach (var (id, type) in factory.GetStates().WithCancellation(cancellationToken))
                {
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
