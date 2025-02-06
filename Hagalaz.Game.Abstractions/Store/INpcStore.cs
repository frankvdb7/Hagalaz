using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Store
{
    /// <summary>
    /// 
    /// </summary>
    public interface INpcStore
    {
        /// <summary>
        /// 
        /// </summary>
        IAsyncEnumerable<INpc> FindAllAsync();
        /// <summary>
        /// Count the npcs in the store
        /// </summary>
        /// <returns></returns>
        ValueTask<int> CountAsync();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        ValueTask<bool> AddAsync(INpc npc);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        ValueTask<bool> RemoveAsync(INpc npc);
        /// <summary>
        /// Finds the specified npc async.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        ValueTask<INpc?> FindAsync(Func<INpc, bool> predicate);
    }
}