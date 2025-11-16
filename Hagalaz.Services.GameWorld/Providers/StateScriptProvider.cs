using System;
using System.Collections.Generic;
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
    public class StateScriptProvider : IStateScriptProvider, IStartupService
    {
        private readonly Dictionary<Type, Type> _scripts = new();
        private readonly Dictionary<string, Type> _stateIdToTypeMap = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly IDefaultStateScriptProvider _defaultStateScriptProvider;
        private readonly ILogger<StateScriptProvider> _logger;

        public StateScriptProvider(
            IServiceProvider serviceProvider, IDefaultStateScriptProvider defaultStateScriptProvider, ILogger<StateScriptProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _defaultStateScriptProvider = defaultStateScriptProvider;
            _logger = logger;
        }

        public Type FindByType(Type stateType) =>
            _scripts.TryGetValue(stateType, out var scriptType) ? scriptType : _defaultStateScriptProvider.GetScriptType();

        public Type? FindByStateId(string stateId) =>
            _stateIdToTypeMap.TryGetValue(stateId, out var stateType) ? stateType : null;

        public IEnumerable<Type> GetAllStateTypes() => _scripts.Keys;

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var scriptFactories = scope.ServiceProvider.GetServices<IStateScriptFactory>();
            foreach (var factory in scriptFactories)
            {
                await foreach (var (stateType, scriptType) in factory.GetScripts().WithCancellation(cancellationToken))
                {
                    if (_scripts.TryAdd(stateType, scriptType))
                    {
                        var stateIdAttribute = stateType.GetCustomAttribute<StateIdAttribute>();
                        if (stateIdAttribute != null)
                        {
                            _stateIdToTypeMap[stateIdAttribute.Id] = stateType;
                        }
                        _logger.LogTrace("Added state script '{StateType}' '{Type}'", stateType.FullName, scriptType.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate state script '{StateType}' '{Type}'", stateType.FullName, scriptType.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} state scripts", _scripts.Count);
        }
    }
}