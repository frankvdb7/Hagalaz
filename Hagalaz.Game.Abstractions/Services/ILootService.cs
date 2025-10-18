using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Loot;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages loot tables.
    /// </summary>
    public interface ILootService
    {
        /// <summary>
        /// Finds the loot table for a specific NPC.
        /// </summary>
        /// <param name="id">The ID of the NPC.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ILootTable"/> if found; otherwise, <c>null</c>.</returns>
        public Task<ILootTable?> FindNpcLootTable(int id);

        /// <summary>
        /// Finds the loot table for a specific item (e.g., a clue scroll casket).
        /// </summary>
        /// <param name="id">The ID of the item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ILootTable"/> if found; otherwise, <c>null</c>.</returns>
        public Task<ILootTable?> FindItemLootTable(int id);

        /// <summary>
        /// Finds the loot table for a specific game object.
        /// </summary>
        /// <param name="id">The ID of the game object.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ILootTable"/> if found; otherwise, <c>null</c>.</returns>
        public Task<ILootTable?> FindGameObjectLootTable(int id);
    }
}