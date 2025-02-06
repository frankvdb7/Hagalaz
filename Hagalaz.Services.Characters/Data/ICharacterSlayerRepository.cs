using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterSlayerRepository
    {
        IQueryable<CharactersSlayerTask> FindById(uint masterId);
    }
}
