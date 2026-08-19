using System;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// Activates character-NPC scripts from the current character scope.
    /// </summary>
    public sealed class CharacterNpcScriptActivator : ICharacterNpcScriptActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public CharacterNpcScriptActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICharacterNpcScript Create(Type scriptType) =>
            (ICharacterNpcScript)_serviceProvider.GetRequiredService(scriptType);
    }
}
