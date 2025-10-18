using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages data for the TzHaar Fight Cave minigame.
    /// </summary>
    public interface ITzHaarCaveService
    {
        /// <summary>
        /// Finds all wave definitions for the TzHaar Fight Cave.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all wave data transfer objects.</returns>
        Task<IReadOnlyList<WaveDto>> FindAllWaves();
    }
}