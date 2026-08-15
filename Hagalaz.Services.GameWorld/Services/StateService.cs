using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Services
{
    public class StateService : IStateService
    {
        private readonly IStateProvider _stateProvider;

        public StateService(IStateProvider stateProvider) => _stateProvider = stateProvider;

        public bool TryCreateState(string stateId, [NotNullWhen(true)] out IState? state) => _stateProvider.TryCreateState(stateId, out state);

        public bool TryGetStateId(IState state, [NotNullWhen(true)] out string? stateId) => _stateProvider.TryGetStateId(state, out stateId);
    }
}
