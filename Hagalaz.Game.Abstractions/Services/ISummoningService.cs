using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Summoning skill data, such as familiar definitions.
    /// </summary>
    public interface ISummoningService
    {
        /// <summary>
        /// Finds a familiar definition by its NPC ID.
        /// </summary>
        /// <param name="npcId">The ID of the familiar NPC.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="SummoningDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<SummoningDto?> FindDefinitionByNpcId(int npcId);

        /// <summary>
        /// Finds a familiar definition by its summoning pouch ID.
        /// </summary>
        /// <param name="pouchId">The item ID of the summoning pouch.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="SummoningDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<SummoningDto?> FindDefinitionByPouchId(int pouchId);

        /// <summary>
        /// Finds a familiar definition by its special move scroll ID.
        /// </summary>
        /// <param name="scrollId">The item ID of the special move scroll.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="SummoningDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<SummoningDto?> FindDefinitionByScrollId(int scrollId);

        /// <summary>
        /// Finds all familiar definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all summoning data transfer objects.</returns>
        Task<IReadOnlyList<SummoningDto>> FindAllDefinitions();
    }
}