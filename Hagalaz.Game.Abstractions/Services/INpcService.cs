using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface INpcService
    {
        /// <summary>
        /// Finds the npc by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        ValueTask<INpc?> FindByIndexAsync(int index);
        INpcDefinition FindNpcDefinitionById(int npcID);
        int GetNpcDefinitionCount();

        /// <summary>
        /// Registers the npc to the world.
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        Task RegisterAsync(INpc npc);

        /// <summary>
        /// Unregisters the npc from the world.
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        Task UnregisterAsync(INpc npc);
    }
}