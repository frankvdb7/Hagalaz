using System;
using System.Collections.Generic;
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

        public DefaultCharacterScriptProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IEnumerable<IDefaultCharacterScript> GetAllScripts()
            => _serviceProvider.GetServices<IDefaultCharacterScript>();

    }
}
