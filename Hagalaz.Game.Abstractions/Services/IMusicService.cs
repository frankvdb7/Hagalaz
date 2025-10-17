using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages music track data.
    /// </summary>
    public interface IMusicService
    {
        /// <summary>
        /// Finds all music track IDs associated with a specific map region.
        /// </summary>
        /// <param name="regionId">The ID of the map region.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of music track IDs for the region.</returns>
        Task<IReadOnlyList<int>> FindMusicIdsByRegionId(int regionId);

        /// <summary>
        /// Finds a music track definition by its index.
        /// </summary>
        /// <param name="index">The index of the music track.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="MusicDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<MusicDto?> FindMusicByIndex(int index);
    }
}