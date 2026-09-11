using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Services;

/// <summary>
/// Accepts an initial load request without making the synchronous map-region API asynchronous.
/// </summary>
public interface IMapRegionLoadRequestSink
{
    void RequestLoad(IMapRegion region);
}
