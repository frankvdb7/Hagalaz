using Hagalaz.Game.Abstractions.Model.Maps;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Accepts map-region load requests without performing the asynchronous load at the caller's boundary.
    /// </summary>
    public interface IMapRegionLoadScheduler
    {
        /// <summary>
        /// Requests loading for a region if it is not already loaded or scheduled.
        /// </summary>
        /// <param name="region">The region to load.</param>
        void RequestLoad(IMapRegion region);

        Task EnsureLoadedAsync(IEnumerable<IMapRegion> regions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a dynamic region whose content is being copied by its creator. The normal loader
        /// never loads a dynamic region: waiters wait for that copy instead.
        /// </summary>
        void BeginDynamicPopulation(IMapRegion region);

        /// <summary>
        /// Ends the registration made by <see cref="BeginDynamicPopulation"/>, releasing waiters.
        /// </summary>
        /// <param name="region">The dynamic region.</param>
        /// <param name="succeeded">Whether the copy finished and the region is ready.</param>
        void CompleteDynamicPopulation(IMapRegion region, bool succeeded);

        /// <summary>
        /// Waits until a dynamic region's copy has finished; throws when it failed. Returns at once
        /// for a region that is not being populated.
        /// </summary>
        Task WaitForDynamicPopulationAsync(IMapRegion region, CancellationToken cancellationToken = default);
    }
}
