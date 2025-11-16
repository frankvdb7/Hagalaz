using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class StateService : IStateService
    {
        private readonly Dictionary<string, Type> _stateIdToTypeMap = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StateService> _logger;

        public StateService(IServiceProvider serviceProvider, ILogger<StateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            LoadStates();
        }

        private void LoadStates()
        {
            var stateTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IState).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var stateType in stateTypes)
            {
                var stateIdAttribute = stateType.GetCustomAttribute<StateIdAttribute>();
                if (stateIdAttribute != null)
                {
                    if (_stateIdToTypeMap.TryAdd(stateIdAttribute.Id, stateType))
                    {
                        _logger.LogTrace("Added state '{StateType}' with ID '{StateId}'", stateType.FullName, stateIdAttribute.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate state ID '{StateId}' for type '{StateType}'", stateIdAttribute.Id, stateType.FullName);
                    }
                }
            }
            _logger.LogInformation("Loaded {Count} states", _stateIdToTypeMap.Count);
        }

        public IState? GetState(string stateId)
        {
            if (_stateIdToTypeMap.TryGetValue(stateId, out var stateType))
            {
                return (IState?)_serviceProvider.GetService(stateType);
            }

            _logger.LogWarning("Could not find state type with ID {stateId}", stateId);
            return null;
        }
    }
}