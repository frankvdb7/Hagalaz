using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Dehydrators;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public class CharacterDehydrationService : ICharacterDehydrationService
    {
        private readonly IEnumerable<ICharacterDehydrator> _dehydrators;

        public CharacterDehydrationService(IEnumerable<ICharacterDehydrator> dehydrators)
        {
            _dehydrators = dehydrators;
        }

        public async Task<CharacterModel> DehydrateAsync(ICharacter character)
        {
            var model = new CharacterModel();
            foreach (var dehydrator in _dehydrators)
            {
                model = await dehydrator.DehydrateAsync(character, model);
            }
            return model;
        }
    }
}
