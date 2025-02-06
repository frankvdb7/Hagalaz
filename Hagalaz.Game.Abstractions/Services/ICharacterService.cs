using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services
{
    public interface ICharacterService
    {
        public ValueTask<ICharacter?> FindByMasterId(uint masterId);
        public ValueTask<ICharacter?> FindByIndex(int index);
        public IAsyncEnumerable<ICharacter> FindAll();
        public ValueTask<bool> AddAsync(ICharacter character);
        public ValueTask<bool> RemoveAsync(ICharacter character);
        public ValueTask<int> CountAsync();
    }
}
