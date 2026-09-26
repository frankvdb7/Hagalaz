using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;

namespace Hagalaz.Services.GameWorld.Services.Cache
{
    public class CachedGameObjectService : IGameObjectService
    {
        private readonly GameObjectService _inner;
        private readonly HybridCache _cache;

        public CachedGameObjectService(GameObjectService inner, HybridCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public IEnumerable<IGameObject> FindByLocation(ILocation location) => _inner.FindByLocation(location);

        public async Task<IGameObjectDefinition> FindGameObjectDefinitionById(int objectId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{Constants.Cache.GameObjectDefinitionCachePrefix}{objectId}";
            return await _cache.GetOrCreateAsync(cacheKey,
                async token => await _inner.FindGameObjectDefinitionById(objectId, token),
                tags: Constants.Cache.GameObjectTags,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyDictionary<int, IGameObjectDefinition>> FindGameObjectDefinitionsByIdsAsync(
            IEnumerable<int> objectIds,
            CancellationToken cancellationToken = default)
        {
            using var resolutionActivity = GameObjectDefinitionResolutionDiagnostics.StartActivity(
                GameObjectDefinitionResolutionDiagnostics.BulkResolutionActivityName);
            resolutionActivity?.SetTag("outcome", "started");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (System.Exception exception)
            {
                GameObjectDefinitionResolutionDiagnostics.RecordFailure(resolutionActivity, exception);
                throw;
            }

            var deduplicateAndSortStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(resolutionActivity);
            var requestedIds = objectIds.Distinct().OrderBy(id => id).ToArray();
            GameObjectDefinitionResolutionDiagnostics.RecordElapsed(
                resolutionActivity,
                "cache.key_deduplicate_sort_duration_ms",
                deduplicateAndSortStart);
            resolutionActivity?.SetTag("requested_definition_count", requestedIds.Length);
            if (requestedIds.Length == 0)
            {
                resolutionActivity?.SetTag("outcome", "empty");
                resolutionActivity?.SetStatus(ActivityStatusCode.Ok);
                return new Dictionary<int, IGameObjectDefinition>();
            }

            var keyGenerationStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(resolutionActivity);
            var cacheKey = $"{Constants.Cache.GameObjectDefinitionCachePrefix}batch:{string.Join(",", requestedIds)}";
            GameObjectDefinitionResolutionDiagnostics.RecordElapsed(
                resolutionActivity,
                "cache.key_generation_duration_ms",
                keyGenerationStart);

            using var cacheActivity = GameObjectDefinitionResolutionDiagnostics.StartActivity(
                GameObjectDefinitionResolutionDiagnostics.CacheLookupActivityName);
            var factoryInvoked = false;
            var factoryStart = 0L;
            var factoryCompleted = 0L;

            try
            {
                var definitions = await _cache.GetOrCreateAsync(cacheKey,
                    async token =>
                    {
                        factoryInvoked = true;
                        factoryStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(cacheActivity);
                        try
                        {
                            return await _inner.FindGameObjectDefinitionsByIdsAsync(requestedIds, token);
                        }
                        finally
                        {
                            factoryCompleted = factoryStart == 0 ? 0 : Stopwatch.GetTimestamp();
                            GameObjectDefinitionResolutionDiagnostics.RecordElapsed(
                                cacheActivity,
                                "cache.factory_duration_ms",
                                factoryStart);
                        }
                    },
                    tags: Constants.Cache.GameObjectTags,
                    cancellationToken: cancellationToken);

                cacheActivity?.SetTag("cache.factory_invoked", factoryInvoked);
                cacheActivity?.SetTag("cache.outcome", factoryInvoked ? "miss" : "hit");
                if (factoryInvoked && factoryCompleted != 0)
                {
                    GameObjectDefinitionResolutionDiagnostics.AddDuration(
                        cacheActivity,
                        "cache.after_factory_duration_ms",
                        Stopwatch.GetElapsedTime(factoryCompleted).TotalMilliseconds);
                }

                var dictionaryStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(resolutionActivity);
                var result = definitions.ToDictionary(pair => pair.Key, pair => (IGameObjectDefinition)pair.Value);
                GameObjectDefinitionResolutionDiagnostics.RecordElapsed(
                    resolutionActivity,
                    "result_dictionary_materialization_duration_ms",
                    dictionaryStart);
                resolutionActivity?.SetTag("result_definition_count", result.Count);
                resolutionActivity?.SetTag("outcome", "success");
                resolutionActivity?.SetStatus(ActivityStatusCode.Ok);
                cacheActivity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (System.Exception exception)
            {
                cacheActivity?.SetTag("cache.factory_invoked", factoryInvoked);
                cacheActivity?.SetTag(
                    "cache.outcome",
                    exception is System.OperationCanceledException ? "cancelled" : "error");
                GameObjectDefinitionResolutionDiagnostics.RecordFailure(cacheActivity, exception);
                GameObjectDefinitionResolutionDiagnostics.RecordFailure(resolutionActivity, exception);
                throw;
            }
        }

        public int GetObjectsCount() => _inner.GetObjectsCount();

        public void UpdateGameObject(GameObjectUpdate gameObjectUpdate) => _inner.UpdateGameObject(gameObjectUpdate);

        public void AnimateGameObject(IGameObject gameObject, IAnimation animation) => _inner.AnimateGameObject(gameObject, animation);
    }
}
