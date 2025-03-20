using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.Caching.Hybrid;

namespace Hagalaz.Services.GameWorld.Services.Cache
{
    public class CachedFishingService : IFishingService
    {
        private readonly FishingService _inner;
        private readonly HybridCache _cache;

        public CachedFishingService(FishingService inner, HybridCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<IFishingSpotTable?> FindSpotByNpcId(int npcId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{Constants.Cache.FishingSpotTableCachePrefix}{npcId}";
            return await _cache.GetOrCreateAsync(cacheKey,
                async token =>
                {
                    var result = await _inner.FindSpotByNpcId(npcId, token);
                    return result;
                },
                tags: Constants.Cache.FishingTags,
                cancellationToken: cancellationToken);
        }

        public async Task<IFishingSpotTable?> FindSpotByNpcIdClickType(int npcId, NpcClickType clickType, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{Constants.Cache.FishingSpotTableCachePrefix}{clickType.ToString().ToLowerInvariant()}:{npcId}";
            return await _cache.GetOrCreateAsync(cacheKey,
                async token =>
                {
                    var result = await _inner.FindSpotByNpcIdClickType(npcId, clickType, token);
                    return result;
                },
                tags: Constants.Cache.FishingTags,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<IFishingSpotTable>> FindAllSpots(CancellationToken cancellationToken = default)
        {
            const string cacheKey = $"{Constants.Cache.FishingSpotTableCachePrefix}all";
            return await _cache.GetOrCreateAsync(cacheKey,
                async token =>
                {
                    var result = await _inner.FindAllSpots(token);
                    return result;
                },
                tags: Constants.Cache.FishingTags,
                cancellationToken: cancellationToken);
        }
    }
}