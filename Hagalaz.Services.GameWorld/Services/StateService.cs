using System;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class StateService : IStateService
    {
        private readonly IStateScriptProvider _stateScriptProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StateService> _logger;

        public StateService(IStateScriptProvider stateScriptProvider, IServiceProvider serviceProvider, ILogger<StateService> logger)
        {
            _stateScriptProvider = stateScriptProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IState? GetState(string stateId)
        {
            var stateType = _stateScriptProvider.FindByStateId(stateId);
            if (stateType == null)
            {
                _logger.LogWarning("Could not find state type with ID {stateId}", stateId);
                return null;
            }

            return (IState?)_serviceProvider.GetService(stateType);
        }
    }
}