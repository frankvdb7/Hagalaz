using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
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
                async token =>
                {
                    var result = await _inner.FindGameObjectDefinitionById(objectId, token);
                    return result;
                },
                tags: Constants.Cache.GameObjectTags,
                cancellationToken: cancellationToken);
        }

        public int GetObjectsCount() => _inner.GetObjectsCount();

        public void UpdateGameObject(GameObjectUpdate gameObjectUpdate) => _inner.UpdateGameObject(gameObjectUpdate);

        public void AnimateGameObject(IGameObject gameObject, IAnimation animation) => _inner.AnimateGameObject(gameObject, animation);
    }
}