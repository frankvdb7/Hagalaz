using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Providers
{
    /// <summary>
    /// Provides management for characters.
    /// </summary>
    public class DefaultCharacterScriptProvider : IDefaultCharacterScriptProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<IDefaultCharacterScript> _characterScripts;

        public DefaultCharacterScriptProvider(IServiceProvider serviceProvider, IEnumerable<IDefaultCharacterScript> characterScripts)
        {
            _serviceProvider = serviceProvider;
            _characterScripts = characterScripts;
        }

        public IEnumerable<IDefaultCharacterScript> GetAllScripts() => _characterScripts.Select(script => (IDefaultCharacterScript)_serviceProvider.GetRequiredService(script.GetType()));

    }
}