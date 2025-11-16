using System;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class StateService : IStateService
    {
        private readonly IStateScriptProvider _stateScriptProvider;
        private readonly ILogger<StateService> _logger;

        public StateService(IStateScriptProvider stateScriptProvider, ILogger<StateService> logger)
        {
            _stateScriptProvider = stateScriptProvider;
            _logger = logger;
        }

        public IState? GetState(string stateId)
        {
            var stateType = _stateScriptProvider.GetAllStateTypes().FirstOrDefault(t => t.Name == stateId);
            if (stateType == null)
            {
                _logger.LogError("Could not find state type with ID {stateId}", stateId);
                return null;
            }

            var state = (IState)Activator.CreateInstance(stateType);
            return state;
        }
    }
}
