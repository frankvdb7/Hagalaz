using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterItemRepository
    {
        public IQueryable<CharactersItem> FindByMasterId(uint masterId);
    }
}
