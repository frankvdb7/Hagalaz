using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions;

public interface IMapRegionLoadRollback
{
    Task ResetUnpublishedLoadAsync(CancellationToken cancellationToken = default);
}
