using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Mining skill data, such as ore, rock, and pickaxe definitions.
    /// </summary>
    public interface IMiningService
    {
        /// <summary>
        /// Finds all ore definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all ore data transfer objects.</returns>
        Task<IReadOnlyList<OreDto>> FindAllOres();

        /// <summary>
        /// Finds an ore definition by its corresponding rock object ID.
        /// </summary>
        /// <param name="rockId">The object ID of the rock.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="OreDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<OreDto?> FindOreByRockId(int rockId);

        /// <summary>
        /// Finds a rock definition by its object ID.
        /// </summary>
        /// <param name="rockId">The object ID of the rock.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="RockDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<RockDto?> FindRockById(int rockId);

        /// <summary>
        /// Finds all pickaxe definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all pickaxe data transfer objects.</returns>
        Task<IReadOnlyList<PickaxeDto>> FindAllPickaxes();

        /// <summary>
        /// Finds all rock definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all rock data transfer objects.</returns>
        Task<IReadOnlyList<RockDto>> FindAllRocks();

        /// <summary>
        /// Finds the loot table for a specific rock.
        /// </summary>
        /// <param name="rockId">The object ID of the rock.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ILootTable"/> if found; otherwise, <c>null</c>.</returns>
        Task<ILootTable?> FindRockLootById(int rockId);
    }
}