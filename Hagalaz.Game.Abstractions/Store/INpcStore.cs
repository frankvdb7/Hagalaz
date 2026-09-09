using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Store
{
    /// <summary>
    /// Defines a contract for a store that manages the persistence and retrieval of NPC instances in the game world.
    /// </summary>
    public interface INpcStore
    {
        /// <summary>
        /// Asynchronously retrieves all NPCs currently in the store.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="INpc"/> instances.</returns>
        IAsyncEnumerable<INpc> FindAllAsync();

        /// <summary>
        /// Asynchronously gets the total number of NPCs in the store.
        /// </summary>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to the NPC count.</returns>
        ValueTask<int> CountAsync();

        /// <summary>
        /// Attempts to add a new NPC to the store.
        /// </summary>
        /// <param name="npc">The NPC to add.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to <c>true</c> if the NPC was added successfully; otherwise, <c>false</c>.</returns>
        ValueTask<bool> AddAsync(INpc npc);

        /// <summary>
        /// Synchronously attempts to add a new NPC to the store.
        /// </summary>
        /// <param name="npc">The NPC to add.</param>
        /// <returns><c>true</c> if the NPC was added successfully; otherwise, <c>false</c>.</returns>
        bool Add(INpc npc);

        /// <summary>
        /// Asynchronously removes an NPC from the store.
        /// </summary>
        /// <param name="npc">The NPC to remove.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to <c>true</c> if the NPC was removed successfully; otherwise, <c>false</c>.</returns>
        ValueTask<bool> RemoveAsync(INpc npc);

        /// <summary>
        /// Synchronously removes an NPC from the store.
        /// </summary>
        /// <param name="npc">The NPC to remove.</param>
        /// <returns><c>true</c> if the NPC was removed successfully; otherwise, <c>false</c>.</returns>
        bool Remove(INpc npc);

        /// <summary>
        /// Asynchronously finds an NPC by its server-side index.
        /// </summary>
        /// <param name="index">The server-side NPC index.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to the matching <see cref="INpc"/>, or <c>null</c> if no NPC is found.</returns>
        ValueTask<INpc?> FindByIndexAsync(int index);
    }
}
