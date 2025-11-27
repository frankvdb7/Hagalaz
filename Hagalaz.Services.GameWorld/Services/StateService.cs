using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class StateService : IStateService
    {
        private readonly IStateProvider _stateProvider;
        private readonly IServiceProvider _serviceProvider;

        public StateService(IStateProvider stateProvider, IServiceProvider serviceProvider)
        {
            _stateProvider = stateProvider;
            _serviceProvider = serviceProvider;
        }

        public IState GetState(string stateId)
        {
            var type = _stateProvider.GetStateById(stateId);

            return (IState)_serviceProvider.GetRequiredService(type);
        }
    }
}