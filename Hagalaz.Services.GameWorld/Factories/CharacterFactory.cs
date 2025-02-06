using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class CharacterFactory : ICharacterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CharacterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICharacter Create(IGameSession session, IGameClient client)
        {
            var serviceScope = _serviceProvider.CreateScope();
            var character = new Character(serviceScope, session, client);
            return character;
        }
    }
}