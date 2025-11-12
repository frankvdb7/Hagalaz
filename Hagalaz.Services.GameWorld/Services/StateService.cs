using System;
using System.Linq;
using System.Reflection;
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

        public Task<Result<IState>> GetStateAsync(string stateId)
        {
            var stateType = _stateScriptProvider.GetAllStateTypes().FirstOrDefault(t => t.GetCustomAttribute<StateIdAttribute>()?.Id == stateId);
            if (stateType == null)
            {
                _logger.LogError("Could not find state type with ID {stateId}", stateId);
                return Task.FromResult(Result.Fail<IState>($"Could not find state type with ID {stateId}"));
            }

            var state = (IState)Activator.CreateInstance(stateType);
            return Task.FromResult(Result.Ok(state));
        }
    }
}
