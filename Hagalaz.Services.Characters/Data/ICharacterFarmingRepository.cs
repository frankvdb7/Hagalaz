using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterFarmingRepository
    {
        public IQueryable<CharactersFarmingPatch> FindById(uint masterId);
    }
}
