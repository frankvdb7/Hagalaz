using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterStateRepository
    {
        IQueryable<CharactersState> FindAll();
    }
}
