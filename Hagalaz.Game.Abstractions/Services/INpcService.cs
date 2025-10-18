using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Non-Player Characters (NPCs) in the game world.
    /// </summary>
    public interface INpcService
    {
        /// <summary>
        /// Finds an NPC by its server index.
        /// </summary>
        /// <param name="index">The server index of the NPC to find.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the <see cref="INpc"/> if found; otherwise, <c>null</c>.</returns>
        ValueTask<INpc?> FindByIndexAsync(int index);

        /// <summary>
        /// Finds an NPC definition by its ID.
        /// </summary>
        /// <param name="npcID">The ID of the NPC definition to find.</param>
        /// <returns>The <see cref="INpcDefinition"/> for the NPC.</returns>
        INpcDefinition FindNpcDefinitionById(int npcID);

        /// <summary>
        /// Gets the total number of unique NPC definitions loaded.
        /// </summary>
        /// <returns>The total count of NPC definitions.</returns>
        int GetNpcDefinitionCount();

        /// <summary>
        /// Registers an NPC, adding it to the game world.
        /// </summary>
        /// <param name="npc">The NPC to register.</param>
        /// <returns>A task that represents the asynchronous registration operation.</returns>
        Task RegisterAsync(INpc npc);

        /// <summary>
        /// Unregisters an NPC, removing it from the game world.
        /// </summary>
        /// <param name="npc">The NPC to unregister.</param>
        /// <returns>A task that represents the asynchronous unregistration operation.</returns>
        Task UnregisterAsync(INpc npc);
    }
}