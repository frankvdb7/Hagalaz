using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages lodestone network data.
    /// </summary>
    public interface ILodestoneService
    {
        /// <summary>
        /// Finds all lodestone definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all lodestone data transfer objects.</returns>
        Task<IReadOnlyList<LodestoneDto>> FindAll();

        /// <summary>
        /// Finds a lodestone definition by its game object ID.
        /// </summary>
        /// <param name="id">The game object ID of the lodestone.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="LodestoneDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<LodestoneDto?> FindByGameObjectId(int id);
    }
}