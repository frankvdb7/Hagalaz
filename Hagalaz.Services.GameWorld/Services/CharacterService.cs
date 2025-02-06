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

        public async ValueTask<bool> AddAsync(ICharacter character) => await _characterStore.AddAsync(character);

        public async ValueTask<bool> RemoveAsync(ICharacter character) => await _characterStore.RemoveAsync(character);
        public async ValueTask<int> CountAsync() => await _characterStore.CountAsync();
        public ValueTask<ICharacter?> FindByIndex(int index) => _characterStore.FindAsync(c => c.Index == index);
        public async ValueTask<ICharacter?> FindByMasterId(uint masterId)
        {
            var character = await _characterStore.FindByIdAsync(masterId);
            if (character != null)
            {
                return character;
            }
            return null;
        }
        public async IAsyncEnumerable<ICharacter> FindAll()
        {
            await foreach (var character in _characterStore.FindAllAsync())
            {
                yield return character;
            }
        }
    }
}
