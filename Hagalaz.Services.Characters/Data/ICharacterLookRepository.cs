using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterLookRepository
    {
        public IQueryable<CharactersLook> FindById(uint masterId);
    }
}
