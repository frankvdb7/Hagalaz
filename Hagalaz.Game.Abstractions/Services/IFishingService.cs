using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Fishing skill data, such as fishing spot definitions.
    /// </summary>
    public interface IFishingService
    {
        /// <summary>
        /// Finds a fishing spot definition by the ID of the NPC that represents the spot.
        /// </summary>
        /// <param name="npcId">The ID of the fishing spot NPC.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="IFishingSpotTable"/> if found; otherwise, <c>null</c>.</returns>
        Task<IFishingSpotTable?> FindSpotByNpcId(int npcId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds a fishing spot definition by the NPC ID and the click option used to interact with it.
        /// </summary>
        /// <param name="npcId">The ID of the fishing spot NPC.</param>
        /// <param name="clickType">The click option used on the NPC.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="IFishingSpotTable"/> if found; otherwise, <c>null</c>.</returns>
        Task<IFishingSpotTable?> FindSpotByNpcIdClickType(int npcId, NpcClickType clickType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds all fishing spot definitions.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all fishing spot tables.</returns>
        Task<IReadOnlyList<IFishingSpotTable>> FindAllSpots(CancellationToken cancellationToken = default);
    }
}