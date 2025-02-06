using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameLogon.Data
{
    public interface ICharacterOffenceRepository
    {
        public IQueryable<CharactersOffence> FindActiveOffencesByMasterIdAsync(uint masterId);
    }
}