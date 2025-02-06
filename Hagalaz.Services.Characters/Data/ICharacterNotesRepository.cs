using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterNotesRepository
    {
        IQueryable<CharactersNote> FindById(uint masterId);
    }
}
