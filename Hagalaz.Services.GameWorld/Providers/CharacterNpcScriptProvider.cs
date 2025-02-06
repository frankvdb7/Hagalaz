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

    public class CharacterNpcScriptProvider : ICharacterNpcScriptProvider, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDefaultCharacterNpcScriptProvider _defaultCharacterNpcScriptProvider;
        private readonly ILogger<CharacterNpcScriptProvider> _logger;
        private readonly Dictionary<int, Type> _scripts;

        public CharacterNpcScriptProvider(IServiceProvider serviceProvider, IDefaultCharacterNpcScriptProvider defaultCharacterNpcScriptProvider, ILogger<CharacterNpcScriptProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _defaultCharacterNpcScriptProvider = defaultCharacterNpcScriptProvider ?? throw new ArgumentNullException(nameof(defaultCharacterNpcScriptProvider));
            _logger = logger;
            _scripts = new Dictionary<int, Type>();
        }

        public Type GetCharacterNpcScriptTypeById(int npcId)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(npcId);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(short.MaxValue, npcId);

            return _scripts.TryGetValue(npcId, out var scriptType) ? scriptType : _defaultCharacterNpcScriptProvider.GetScriptType();
        }

        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var scriptFactories = scope.ServiceProvider.GetServices<ICharacterNpcScriptFactory>();
            foreach (var factory in scriptFactories)
            {
                await foreach (var (scriptId, scriptType) in factory.GetScripts())
                {
                    if (_scripts.TryAdd(scriptId, scriptType))
                    {
                        _logger.LogTrace("Added character npc script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate character npc script '{Id}' '{Type}'", scriptId, scriptType.FullName);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} character npc scripts", _scripts.Count);
        }
    }
}