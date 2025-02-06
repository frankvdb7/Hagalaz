using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IGameObjectLootRepository
    {
        public IQueryable<GameobjectLoot> FindAll();
    }
}
