using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Microsoft.Extensions.Caching.Hybrid;

namespace Hagalaz.Services.GameWorld.Services.Cache
{
    public class CachedSlayerService : ISlayerService
    {
        private readonly SlayerService _inner;
        private readonly HybridCache _cache;

        public CachedSlayerService(SlayerService inner, HybridCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<ISlayerTaskDefinition?> FindSlayerTaskDefinition(int taskID, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{Constants.Cache.SlayerTaskDefinitionCachePrefix}{taskID}";
            return await _cache.GetOrCreateAsync(cacheKey,
                async token =>
                {
                    var result = await _inner.FindSlayerTaskDefinition(taskID, token);
                    return result;
                },
                tags: Constants.Cache.SlayerTags,
                cancellationToken: cancellationToken);
        }

        public async Task<ISlayerMasterTable?> FindSlayerMasterTableByNpcId(int npcId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{Constants.Cache.SlayerMasterTableCachePrefix}{npcId}";
            return await _cache.GetOrCreateAsync(cacheKey,
                async token =>
                {
                    var result = await _inner.FindSlayerMasterTableByNpcId(npcId, token);
                    return result;
                },
                tags: Constants.Cache.SlayerTags,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<ISlayerMasterTable>> FindAllSlayerMasterTables(CancellationToken cancellationToken = default)
        {
            const string cacheKey = $"{Constants.Cache.SlayerMasterTableCachePrefix}all";
            return await _cache.GetOrCreateAsync(cacheKey,
                async token =>
                {
                    var result = await _inner.FindAllSlayerMasterTables(token);
                    return result;
                },
                tags: Constants.Cache.SlayerTags,
                cancellationToken: cancellationToken);
        }
    }
}