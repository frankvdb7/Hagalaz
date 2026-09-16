using System;
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
        private readonly IEntityStore _entityStore;

        public CharacterService(ICharacterStore characterStore, IEntityStore entityStore)
        {
            _characterStore = characterStore;
            _entityStore = entityStore;
        }

        public async ValueTask<bool> AddAsync(ICharacter character)
        {
            if (!await _characterStore.AddAsync(character))
            {
                return false;
            }

            try
            {
                _entityStore.Add(character);
                return true;
            }
            catch
            {
                await _characterStore.RemoveAsync(character);
                throw;
            }
        }

        public async ValueTask<bool> RemoveAsync(ICharacter character)
        {
            if (!await _characterStore.RemoveAsync(character))
            {
                return false;
            }

            if (!_entityStore.Remove(character))
            {
                await _characterStore.AddAsync(character);
                throw new InvalidOperationException($"Character '{character.MasterId}' was missing from the entity store during removal.");
            }

            return true;
        }

        public bool Remove(ICharacter character)
        {
            if (!_characterStore.Remove(character))
            {
                return false;
            }

            if (!_entityStore.Remove(character))
            {
                _characterStore.AddAsync(character).AsTask().GetAwaiter().GetResult();
                throw new InvalidOperationException($"Character '{character.MasterId}' was missing from the entity store during removal.");
            }

            return true;
        }
        public ValueTask<int> CountAsync() => _characterStore.CountAsync();
        public ValueTask<ICharacter?> FindByIndex(int index) => _characterStore.FindByIndexAsync(index);
        public ValueTask<ICharacter?> FindByMasterId(uint masterId) => _characterStore.FindByIdAsync(masterId);
    }
}
