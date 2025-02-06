using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Authorization.Data
{
    public interface ICharacterOffenceRepository
    {
        public IQueryable<CharactersOffence> FindOffenceByMasterId(uint masterId);
    }
}