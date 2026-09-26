using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Diagnostics;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Metrics;
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
            using var resolutionActivity = GameObjectDefinitionResolutionDiagnostics.StartActivity();
            var resolutionStart = Stopwatch.GetTimestamp();
            var resolutionOutcome = "failure";
            int? batchSize = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requestedIds = objectIds.Distinct().OrderBy(id => id).ToArray();
                batchSize = requestedIds.Length;
                resolutionActivity?.SetTag("requested_definition_count", requestedIds.Length);
                if (requestedIds.Length == 0)
                {
                    resolutionOutcome = "empty";
                    resolutionActivity?.SetTag("result_definition_count", 0);
                    resolutionActivity?.SetTag("outcome", resolutionOutcome);
                    resolutionActivity?.SetStatus(ActivityStatusCode.Ok);
                    return new Dictionary<int, IGameObjectDefinition>();
                }

                var cacheKey = $"{Constants.Cache.GameObjectDefinitionCachePrefix}batch:{string.Join(",", requestedIds)}";
                var factoryInvoked = false;
                IReadOnlyDictionary<int, GameObjectDefinition> definitions;
                try
                {
                    definitions = await _cache.GetOrCreateAsync(cacheKey,
                        async token =>
                        {
                            factoryInvoked = true;
                            return await _inner.FindGameObjectDefinitionsByIdsAsync(requestedIds, token);
                        },
                        tags: Constants.Cache.GameObjectTags,
                        cancellationToken: cancellationToken);
                }
                catch (Exception exception)
                {
                    GameWorldMetrics.RecordDefinitionCacheLookup(exception is OperationCanceledException ? "cancelled" : "error");
                    throw;
                }

                var cacheOutcome = factoryInvoked ? "miss" : "hit";
                resolutionActivity?.SetTag("cache.outcome", cacheOutcome);
                GameWorldMetrics.RecordDefinitionCacheLookup(cacheOutcome);

                var result = definitions.ToDictionary(pair => pair.Key, pair => (IGameObjectDefinition)pair.Value);
                resolutionActivity?.SetTag("result_definition_count", result.Count);
                resolutionOutcome = "success";
                resolutionActivity?.SetTag("outcome", resolutionOutcome);
                resolutionActivity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception exception)
            {
                resolutionOutcome = exception is OperationCanceledException ? "cancelled" : "failure";
                GameObjectDefinitionResolutionDiagnostics.RecordFailure(resolutionActivity, exception);
                throw;
            }
            finally
            {
                GameWorldMetrics.RecordDefinitionResolution(
                    resolutionOutcome,
                    batchSize,
                    Stopwatch.GetElapsedTime(resolutionStart).TotalSeconds);
            }
        }

        public int GetObjectsCount() => _inner.GetObjectsCount();

        public void UpdateGameObject(GameObjectUpdate gameObjectUpdate) => _inner.UpdateGameObject(gameObjectUpdate);

        public void AnimateGameObject(IGameObject gameObject, IAnimation animation) => _inner.AnimateGameObject(gameObject, animation);
    }
}
