using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IShopRepository
    {
        IQueryable<Shop> FindById(int shopId);
    }
}