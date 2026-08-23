using Hagalaz.Game.Abstractions.Model.Maps;

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
    }
}
