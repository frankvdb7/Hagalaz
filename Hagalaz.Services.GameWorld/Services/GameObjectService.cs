using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameObjectService : IGameObjectService
    {
        private readonly IMapRegionService _regionService;
        private readonly GameObjectStore _gameObjectStore;
        private readonly ITypeDecoder<IGameObjectDefinition> _objectDecoder;

        public GameObjectService(IMapRegionService regionService, GameObjectStore gameObjectStore, ITypeDecoder<IGameObjectDefinition> objectDecoder)
        {
            _regionService = regionService;
            _gameObjectStore = gameObjectStore;
            _objectDecoder = objectDecoder;
        }

        public IEnumerable<IGameObject> FindByLocation(ILocation location)
        {
            var region = _regionService.GetOrCreateMapRegion(location.RegionId, location.Dimension, true);
            foreach (var gameObject in region.FindAllGameObjects().Where(gameObject => gameObject.Location.Equals(location)))
            {
                yield return gameObject;
            }
        }

        public IEnumerable<IGameObject> FindAll(GameObjectFindAll gameObjectFindAll)
        {
            var region = _regionService.GetOrCreateMapRegion(gameObjectFindAll.Location.RegionId, gameObjectFindAll.Location.Dimension, true);
            foreach (var gameObject in region.FindAllGameObjects().Where(gameObject => gameObject.Id == gameObjectFindAll.Id))
            {
                yield return gameObject;
            }
        }

        public void UpdateGameObject(GameObjectUpdate gameObjectUpdate)
        {
            var gameObject = gameObjectUpdate.Instance;
            if (gameObject is not GameObject go)
            {
                return;
            }

            var region = _regionService.GetOrCreateMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension, true);
            if (go.Id != gameObjectUpdate.Id)
            {
                go.Id = gameObjectUpdate.Id;
                go.IsStatic = false;
                region.QueueUpdate(new AddGameObjectUpdate(gameObject));
            }

            if (go.Rotation != gameObjectUpdate.Rotation)
            {
                region.UnFlagCollision(go);
                go.Rotation = gameObjectUpdate.Rotation;
                go.IsStatic = false;
                region.FlagCollision(go);
                region.QueueUpdate(new AddGameObjectUpdate(gameObject));
            }
        }

        public void AnimateGameObject(IGameObject gameObject, IAnimation animation)
        {
            var region = _regionService.GetOrCreateMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension, true);
            region.QueueUpdate(new SetGameObjectAnimationUpdate(gameObject, animation));
        }

        public IGameObjectDefinition FindGameObjectDefinitionById(int objectID) => _gameObjectStore.GetOrAdd(objectID);

        public int GetObjectsCount() => _objectDecoder.ArchiveSize;
    }
}