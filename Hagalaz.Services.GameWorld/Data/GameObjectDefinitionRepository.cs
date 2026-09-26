using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Data.Entities;
using Hagalaz.Data;
using Hagalaz.Services.Common.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Data
{
    public class GameObjectDefinitionRepository : RepositoryBase<GameobjectDefinition>, IGameObjectDefinitionRepository
    {
        public GameObjectDefinitionRepository(HagalazDbContext dbContext)
            : base(dbContext)
        {
        }

        public async Task<Dictionary<uint, GameObjectDefinitionOverride>> FindOverridesByIdsAsync(
            IReadOnlyCollection<uint> objectIds,
            CancellationToken cancellationToken = default)
        {
            using var activity = GameObjectDefinitionResolutionDiagnostics.StartActivity(
                GameObjectDefinitionResolutionDiagnostics.RepositoryActivityName);
            activity?.SetTag("requested_id_count", objectIds.Count);

            if (objectIds.Count == 0)
            {
                activity?.SetTag("materialized_override_count", 0);
                activity?.SetTag("outcome", "empty");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return new Dictionary<uint, GameObjectDefinitionOverride>();
            }

            try
            {
                var queryConstructionStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(activity);
                var requestedIds = objectIds.Distinct().ToArray();
                var query = FindAll()
                    .AsNoTracking()
                    .Where(definition => requestedIds.Contains(definition.GameobjectId))
                    .Select(definition => new GameObjectDefinitionOverride(
                        definition.GameobjectId,
                        definition.Examine,
                        definition.GameobjectLootId));
                GameObjectDefinitionResolutionDiagnostics.RecordElapsed(
                    activity,
                    "query_construction_duration_ms",
                    queryConstructionStart);

                var result = await query.ToDictionaryAsync(definition => definition.Id, cancellationToken);
                activity?.SetTag("unique_requested_id_count", requestedIds.Length);
                activity?.SetTag("materialized_override_count", result.Count);
                activity?.SetTag("outcome", "success");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (System.Exception exception)
            {
                GameObjectDefinitionResolutionDiagnostics.RecordFailure(activity, exception);
                throw;
            }
        }
    }
}
