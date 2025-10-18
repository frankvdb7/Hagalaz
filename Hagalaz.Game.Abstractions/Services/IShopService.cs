using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Features.Shops;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages shops.
    /// </summary>
    public interface IShopService
    {
        /// <summary>
        /// Gets a shop by its unique identifier.
        /// </summary>
        /// <param name="shopId">The ID of the shop to retrieve.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the <see cref="IShop"/> if found; otherwise, <c>null</c>.</returns>
        ValueTask<IShop?> GetShopByIdAsync(int shopId);
    }
}