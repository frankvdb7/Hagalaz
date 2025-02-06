using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICraftingService
    {
        /// <summary>
        /// Gets the gem definition index by resource identifier.
        /// </summary>
        /// <param name="resourceID">The resource identifier.</param>
        /// <returns></returns>
        Task<GemDto?> FindGemByResourceID(int resourceID);

        /// <summary>
        /// Gets the leahter index by product identifier.
        /// </summary>
        /// <param name="productID">The product identifier.</param>
        /// <returns></returns>
        Task<LeatherDto?> FindLeatherByProductId(int productID);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<TanDto>> FindAllTan();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<LeatherDto>> FindAllLeather();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jewelryType"></param>
        /// <returns></returns>
        Task<IReadOnlyList<JewelryDto>> FindAllJewelry(JewelryDto.JewelryType jewelryType);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<SilverDto>> FindAllSilver();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<GemDto>> FindAllGems();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<SpinDto>> FindAllSpin();

        /// <summary>
        /// Gets the spin index by product identifier.
        /// </summary>
        /// <param name="productID">The product identifier.</param>
        /// <returns></returns>
        Task<SpinDto?> FindSpinByProductID(int productID);

        /// <summary>
        /// Gets the tan index by product identifier.
        /// </summary>
        /// <param name="productID">The product identifier.</param>
        /// <returns></returns>
        Task<TanDto?> FindTanByProductID(int productID);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<PotteryDto>> FindAllPottery();

        /// <summary>
        /// Gets the pottery index by product identifier.
        /// </summary>
        /// <param name="productID">The product identifier.</param>
        /// <returns></returns>
        Task<PotteryDto?> FindPotteryByProductID(int productID);

        /// <summary>
        /// Gets the pottery index by final product identifier.
        /// </summary>
        /// <param name="finalProductID">The final product identifier.</param>
        /// <returns></returns>
        Task<PotteryDto?> FindPotteryByFinalProductID(int finalProductID);
    }
}