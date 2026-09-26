using System.Collections.Generic;
using System;
using System.Diagnostics;
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
using Hagalaz.Services.GameWorld.Diagnostics;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates;

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
            var definitions = await FindGameObjectDefinitionsByIdsAsync([objectId], cancellationToken);
            return definitions[objectId];
        }

        public async Task<Dictionary<int, GameObjectDefinition>> FindGameObjectDefinitionsByIdsAsync(
            IEnumerable<int> objectIds,
            CancellationToken cancellationToken = default)
        {
            using var activity = GameObjectDefinitionResolutionDiagnostics.StartActivity(
                GameObjectDefinitionResolutionDiagnostics.CompositionActivityName);
            activity?.SetTag("outcome", "started");

            var inputProcessingStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(activity);
            var requestedIds = objectIds.Distinct().ToArray();
            var databaseIds = requestedIds.Where(id => id >= 0).Select(id => (uint)id).Distinct().ToArray();
            GameObjectDefinitionResolutionDiagnostics.RecordElapsed(
                activity,
                "input_processing_duration_ms",
                inputProcessingStart);
            activity?.SetTag("requested_definition_count", requestedIds.Length);
            activity?.SetTag("database_definition_id_count", databaseIds.Length);

            try
            {
                var overrides = databaseIds.Length == 0
                    ? new Dictionary<uint, GameObjectDefinitionOverride>()
                    : await _gameObjectDefinitionRepository.FindOverridesByIdsAsync(databaseIds, cancellationToken);

                var dictionaryStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(activity);
                var definitions = new Dictionary<int, GameObjectDefinition>(requestedIds.Length);
                GameObjectDefinitionResolutionDiagnostics.RecordElapsed(
                    activity,
                    "definition_dictionary_create_duration_ms",
                    dictionaryStart);

                var providerDuration = 0d;
                var compositionDuration = 0d;
                foreach (var objectId in requestedIds)
                {
                    var providerStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(activity);
                    var definition = _objectProvider.Get(objectId);
                    if (providerStart != 0)
                    {
                        providerDuration += Stopwatch.GetElapsedTime(providerStart).TotalMilliseconds;
                    }

                    var compositionStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(activity);
                    if (objectId >= 0 && overrides.TryGetValue((uint)objectId, out var databaseDefinition))
                    {
                        definition.Examine = databaseDefinition.Examine;
                        definition.LootTableId = databaseDefinition.LootTableId ?? 0;
                    }

                    definitions.Add(objectId, definition);
                    if (compositionStart != 0)
                    {
                        compositionDuration += Stopwatch.GetElapsedTime(compositionStart).TotalMilliseconds;
                    }
                }

                activity?.SetTag("archive.provider_lookup_count", requestedIds.Length);
                activity?.SetTag("archive.provider_lookup_duration_ms", providerDuration);
                activity?.SetTag("definition.override_and_dictionary_duration_ms", compositionDuration);
                activity?.SetTag("result_definition_count", definitions.Count);
                activity?.SetTag("outcome", "success");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return definitions;
            }
            catch (Exception exception)
            {
                GameObjectDefinitionResolutionDiagnostics.RecordFailure(activity, exception);
                throw;
            }
        }

        public int GetObjectsCount() => _objectProvider.ArchiveSize;
    }
}
