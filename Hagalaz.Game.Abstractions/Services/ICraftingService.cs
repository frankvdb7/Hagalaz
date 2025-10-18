using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Crafting skill data, such as recipes and material definitions.
    /// </summary>
    public interface ICraftingService
    {
        /// <summary>
        /// Finds a gem definition by its resource item ID.
        /// </summary>
        /// <param name="resourceID">The item ID of the uncut gem.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="GemDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<GemDto?> FindGemByResourceID(int resourceID);

        /// <summary>
        /// Finds a leather definition by its product item ID.
        /// </summary>
        /// <param name="productID">The item ID of the crafted leather item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="LeatherDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<LeatherDto?> FindLeatherByProductId(int productID);

        /// <summary>
        /// Finds all leather tanning definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all tan data transfer objects.</returns>
        Task<IReadOnlyList<TanDto>> FindAllTan();

        /// <summary>
        /// Finds all leather crafting definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all leather data transfer objects.</returns>
        Task<IReadOnlyList<LeatherDto>> FindAllLeather();

        /// <summary>
        /// Finds all jewelry crafting definitions for a specific type of jewelry.
        /// </summary>
        /// <param name="jewelryType">The type of jewelry (e.g., ring, necklace).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all jewelry data transfer objects for the specified type.</returns>
        Task<IReadOnlyList<JewelryDto>> FindAllJewelry(JewelryDto.JewelryType jewelryType);

        /// <summary>
        /// Finds all silver crafting definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all silver data transfer objects.</returns>
        Task<IReadOnlyList<SilverDto>> FindAllSilver();

        /// <summary>
        /// Finds all gem cutting definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all gem data transfer objects.</returns>
        Task<IReadOnlyList<GemDto>> FindAllGems();

        /// <summary>
        /// Finds all spinning wheel definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all spin data transfer objects.</returns>
        Task<IReadOnlyList<SpinDto>> FindAllSpin();

        /// <summary>
        /// Finds a spinning definition by its product item ID.
        /// </summary>
        /// <param name="productID">The item ID of the spun item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="SpinDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<SpinDto?> FindSpinByProductID(int productID);

        /// <summary>
        /// Finds a leather tanning definition by its product item ID.
        /// </summary>
        /// <param name="productID">The item ID of the tanned leather.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="TanDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<TanDto?> FindTanByProductID(int productID);

        /// <summary>
        /// Finds all pottery crafting definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all pottery data transfer objects.</returns>
        Task<IReadOnlyList<PotteryDto>> FindAllPottery();

        /// <summary>
        /// Finds a pottery definition by its unfired product item ID.
        /// </summary>
        /// <param name="productID">The item ID of the unfired pottery item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="PotteryDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<PotteryDto?> FindPotteryByProductID(int productID);

        /// <summary>
        /// Finds a pottery definition by its fired product item ID.
        /// </summary>
        /// <param name="finalProductID">The item ID of the fired pottery item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="PotteryDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<PotteryDto?> FindPotteryByFinalProductID(int finalProductID);
    }
}