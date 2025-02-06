using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Features.Shops;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IShopService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        ValueTask<IShop?> GetShopByIdAsync(int shopId);
    }
}