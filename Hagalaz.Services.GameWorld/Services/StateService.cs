using System;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Services
{
    public class StateService : IStateService
    {
        private readonly StateProvider _stateProvider;
        private readonly IServiceProvider _serviceProvider;

        public StateService(StateProvider stateProvider, IServiceProvider serviceProvider)
        {
            _stateProvider = stateProvider;
            _serviceProvider = serviceProvider;
        }

        public bool TryCreateState(string stateId, [NotNullWhen(true)] out IState? state)
        {
            state = null;
            if (!_stateProvider.TryGetStateType(stateId, out var stateType))
            {
                return false;
            }

            state = (IState)_serviceProvider.GetRequiredService(stateType);
            return true;
        }

        public bool TryGetStateId(IState state, [NotNullWhen(true)] out string? stateId) => _stateProvider.TryGetStateId(state, out stateId);
    }
}
