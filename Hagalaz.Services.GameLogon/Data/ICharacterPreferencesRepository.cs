using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameLogon.Data
{
    public interface ICharacterPreferencesRepository
    {
        IQueryable<CharactersPreference> FindById(uint masterId);
    }
}