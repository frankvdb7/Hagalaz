using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterStore _characterStore;

        public CharacterService(ICharacterStore characterStore)
        {
            _characterStore = characterStore;
        }

        public ValueTask<bool> AddAsync(ICharacter character) => _characterStore.AddAsync(character);

        public ValueTask<bool> RemoveAsync(ICharacter character) => _characterStore.RemoveAsync(character);
        public ValueTask<int> CountAsync() => _characterStore.CountAsync();
        public ValueTask<ICharacter?> FindByIndex(int index) => _characterStore.FindByIndexAsync(index);
        public ValueTask<ICharacter?> FindByMasterId(uint masterId) => _characterStore.FindByIdAsync(masterId);
    }
}
