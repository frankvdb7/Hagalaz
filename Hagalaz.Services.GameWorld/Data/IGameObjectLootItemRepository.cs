using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IGameObjectLootItemRepository
    {
        public IQueryable<GameobjectLootItem> FindAll();
    }
}
