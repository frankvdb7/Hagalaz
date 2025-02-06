using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{

    public class ShopRepository : RepositoryBase<Shop>, IShopRepository
    {

        public ShopRepository(HagalazDbContext dbContext) : base(dbContext)
        {
        }

        public IQueryable<Shop> FindById(int shopId) => FindAll().Where(shop => shop.Id == shopId);
    }
}