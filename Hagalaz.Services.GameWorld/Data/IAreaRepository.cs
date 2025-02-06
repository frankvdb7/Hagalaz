using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IAreaRepository
    {
        IQueryable<Area> FindAll();
    }
}
