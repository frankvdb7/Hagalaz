using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterRepository
    {
        public IQueryable<Character> FindById(uint masterId);
    }
}
