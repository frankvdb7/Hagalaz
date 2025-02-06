using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterMusicRepository
    {
        IQueryable<CharactersMusic> FindById(uint masterId);
    }
}
