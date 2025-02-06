using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface INpcBonusesRepository
    {
        public IQueryable<NpcBonuses> FindAll();
    }
}