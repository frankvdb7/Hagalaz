using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface INpcLootRepository
    {
        public IQueryable<NpcLoot> FindAll();
    }
}
