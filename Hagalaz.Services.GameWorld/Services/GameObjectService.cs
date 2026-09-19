using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameObjectService
    {
        private readonly IMapRegionService _regionService;
        private readonly IGameObjectDefinitionRepository _gameObjectDefinitionRepository;
        private readonly ITypeProvider<GameObjectDefinition> _objectProvider;

        public GameObjectService(
            IMapRegionService regionService, IGameObjectDefinitionRepository gameObjectDefinitionRepository,
            ITypeProvider<GameObjectDefinition> objectProvider)
        {
            _regionService = regionService;
            _gameObjectDefinitionRepository = gameObjectDefinitionRepository;
            _objectProvider = objectProvider;
        }

        public IEnumerable<IGameObject> FindByLocation(ILocation location)
        {
            var region = _regionService.FindMapRegion(location.RegionId, location.Dimension);
            if (region is null)
            {
                yield break;
            }

            foreach (var gameObject in region.FindAllGameObjects().Where(gameObject => gameObject.Location.Equals(location)))
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

            if (go.Id != gameObjectUpdate.Id)
            {
                go.Id = gameObjectUpdate.Id;
                go.IsStatic = false;
                _regionService.QueueUpdate(new AddGameObjectUpdate(gameObject));
            }

            if (go.Rotation == gameObjectUpdate.Rotation)
            {
                return;
            }

            _regionService.UnFlagCollision(go);
            go.Rotation = gameObjectUpdate.Rotation;
            go.IsStatic = false;
            _regionService.FlagCollision(go);
            _regionService.QueueUpdate(new AddGameObjectUpdate(gameObject));
        }

        public void AnimateGameObject(IGameObject gameObject, IAnimation animation)
        {
            _regionService.QueueUpdate(new SetGameObjectAnimationUpdate(gameObject, animation));
        }

        public async Task<GameObjectDefinition> FindGameObjectDefinitionById(int objectId, CancellationToken cancellationToken = default)
        {
            var definition = _objectProvider.Get(objectId);
            var databaseDefinition = await _gameObjectDefinitionRepository.FindAll()
                .Where(g => g.GameobjectId == objectId)
                .FirstOrDefaultAsync(cancellationToken);

            if (databaseDefinition == null)
            {
                return definition;
            }

            definition.Examine = databaseDefinition.Examine;
            definition.LootTableId = databaseDefinition.GameobjectLootId ?? 0;
            return definition;
        }

        public int GetObjectsCount() => _objectProvider.ArchiveSize;
    }
}
