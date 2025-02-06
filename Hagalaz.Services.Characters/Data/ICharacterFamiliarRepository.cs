using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterFamiliarRepository
    {
        IQueryable<CharactersFamiliar> FindById(uint masterId);
    }
}
