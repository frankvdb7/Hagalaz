using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Services.GameWorld.Data
{
    /// <summary>
    /// Prepares the base map content of a region from the cache and configured object spawns.
    /// </summary>
    public interface IMapRegionTemplateLoader
    {
        Task<MapRegionTemplate> LoadAsync(int regionId, int[] xteaKeys, CancellationToken cancellationToken = default);
    }
}
