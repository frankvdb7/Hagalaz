using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Hydrators;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public class CharacterHydrationService : ICharacterHydrationService
    {
        private readonly IEnumerable<ICharacterHydrator> _hydrators;

        public CharacterHydrationService(IEnumerable<ICharacterHydrator> hydrators)
        {
            _hydrators = hydrators;
        }

        public bool Hydrate(ICharacter character, CharacterModel model)
        {
            foreach (var hydrator in _hydrators)
            {
                hydrator.Hydrate(character, model);
            }
            return true;
        }
    }
}
