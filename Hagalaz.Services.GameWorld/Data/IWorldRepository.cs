using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IWorldRepository
    {
        public IQueryable<World> FindWorldById(int worldId);
    }
}