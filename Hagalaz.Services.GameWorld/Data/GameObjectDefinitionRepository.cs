using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Data.Entities;
using Hagalaz.Data;
using Hagalaz.Services.Common.Data;
using Hagalaz.Services.GameWorld.Data.Model;
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
            if (objectIds.Count == 0)
            {
                return new Dictionary<uint, GameObjectDefinitionOverride>();
            }

            var requestedIds = objectIds.Distinct().ToArray();
            var query = FindAll()
                .AsNoTracking()
                .Where(definition => requestedIds.Contains(definition.GameobjectId))
                .Select(definition => new GameObjectDefinitionOverride(
                    definition.GameobjectId,
                    definition.Examine,
                    definition.GameobjectLootId));

            return await query.ToDictionaryAsync(definition => definition.Id, cancellationToken);
        }
    }
}
