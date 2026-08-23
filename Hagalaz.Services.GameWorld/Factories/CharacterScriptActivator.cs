using System;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// Activates registered character scripts from the current character scope.
    /// </summary>
    public sealed class CharacterScriptActivator : ICharacterScriptActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public CharacterScriptActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TScript Create<TScript>() where TScript : class, ICharacterScript =>
            _serviceProvider.GetRequiredService<TScript>();
    }
}
