using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IGameObjectDefinitionRepository
    {
        Task<Dictionary<uint, GameObjectDefinitionOverride>> FindOverridesByIdsAsync(
            IReadOnlyCollection<uint> objectIds,
            CancellationToken cancellationToken = default);
    }
}
