using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterItemLookRepository
    {
        public IQueryable<CharactersItemsLook> FindById(uint masterId);
    }
}
