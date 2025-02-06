using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ILodestoneRepository
    {
        public IQueryable<GameobjectLodestone> FindAll();
    }
}
