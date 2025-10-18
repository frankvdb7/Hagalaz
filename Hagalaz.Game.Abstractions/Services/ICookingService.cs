using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Cooking skill data, such as food definitions.
    /// </summary>
    public interface ICookingService
    {
        /// <summary>
        /// Finds all cooked food definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all food data transfer objects.</returns>
        Task<IReadOnlyList<FoodDto>> FindAllFood();

        /// <summary>
        /// Finds all raw food definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all raw food data transfer objects.</returns>
        Task<IReadOnlyList<RawFoodDto>> FindAllRawFood();

        /// <summary>
        /// Finds a cooked food definition by its item ID.
        /// </summary>
        /// <param name="itemId">The item ID of the cooked food.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="FoodDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<FoodDto?> FindFoodById(int itemId);

        /// <summary>
        /// Finds a raw food definition by its item ID.
        /// </summary>
        /// <param name="itemId">The item ID of the raw food.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="RawFoodDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<RawFoodDto?> FindRawFoodById(int itemId);
    }
}