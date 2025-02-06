using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public class NpcScriptProvider : INpcScriptProvider, IStartupService
    {
        private readonly ILogger<NpcScriptProvider> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDefaultNpcScriptProvider _defaultNpcScriptProvider;
        private readonly Dictionary<int, Type> _scripts = new();

        public NpcScriptProvider(ILogger<NpcScriptProvider> logger, IServiceProvider serviceProvider, IDefaultNpcScriptProvider defaultNpcScriptProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _defaultNpcScriptProvider = defaultNpcScriptProvider ?? throw new ArgumentNullException(nameof(defaultNpcScriptProvider));
        }

        public Type GetNpcScriptTypeById(int npcId)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(npcId);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(npcId, short.MaxValue);
            return _scripts.TryGetValue(npcId, out var scriptType) ? scriptType : _defaultNpcScriptProvider.GetScriptType();
        }

        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var scriptFactories = scope.ServiceProvider.GetServices<INpcScriptFactory>();
            foreach (var factory in scriptFactories)
            {
                await foreach (var (scriptId, scriptType) in factory.GetScripts())
                {
                    if (_scripts.TryAdd(scriptId, scriptType))
                    {
                        _logger.LogTrace("Added npc script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate npc script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} npc scripts", _scripts.Count);
        }
    }
}