using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMapRegionLoader
    {
        public Task LoadAsync(IMapRegion mapRegion, CancellationToken cancellationToken = default);
    }
}
