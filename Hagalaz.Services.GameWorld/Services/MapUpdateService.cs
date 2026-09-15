using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Orchestrates synchronous map updates at the character render boundary.
    /// </summary>
    public sealed class MapUpdateService : IMapUpdateService
    {
        private readonly IMapRegionLoadScheduler _regionLoadScheduler;
        private readonly IMapRegionService _regionService;
        private readonly ICharacterStore _characterStore;
        // UpdateMap and the queued completion both run through the creature's game task
        // boundary, so this service-local table can remain a plain collection.
        private readonly Dictionary<ICharacter, PendingRegionUpdate> _pendingRegionUpdates = [];

        private sealed class PendingRegionUpdate(IReadOnlyList<IMapRegion> initializingRegions)
        {
            public IReadOnlyList<IMapRegion> InitializingRegions { get; } = initializingRegions;
            public HashSet<IMapRegion> SentRegions { get; } = [];
        }

        public MapUpdateService(
            IMapRegionLoadScheduler regionLoadScheduler,
            IMapRegionService regionService,
            ICharacterStore characterStore)
        {
            _regionLoadScheduler = regionLoadScheduler;
            _regionService = regionService;
            _characterStore = characterStore;
        }

        public void UpdateMap(ICharacter character, bool forceUpdate, bool renderViewPort = false)
        {
            var viewport = character.Viewport;
            if (viewport.VisibleRegions.Count == 0 || viewport.ShouldRebuild())
            {
                viewport.RebuildView();
            }

            viewport.RefreshVisibleRegions();
            var visibleRegions = viewport.VisibleRegions;

            if (viewport.NeedsDynamicDraw())
            {
                character.Session.SendMessage(new DrawDynamicMapMessage());
            }
            else
            {
                character.Session.SendMessage(new DrawStandardMapMessage
                {
                    MapSizeIndex = viewport.MapSize.Type,
                    RenderViewport = renderViewPort,
                    ForceUpdate = forceUpdate,
                    CharacterIndex = character.Index,
                    CharacterLocation = character.Location,
                    RegionPartX = viewport.ViewLocation.RegionPartX,
                    RegionPartY = viewport.ViewLocation.RegionPartY,
                    VisibleRegionXteaKeys = visibleRegions.Select(region => region.XteaKeys).ToList()
                });
            }

            var initializingRegions = visibleRegions.Where(region => region.State == MapRegionState.Initializing).ToArray();
            _pendingRegionUpdates.TryGetValue(character, out var pendingUpdate);

            foreach (var region in visibleRegions)
            {
                if (region.State == MapRegionState.Initializing)
                {
                    _regionLoadScheduler.RequestLoad(region);
                }
                else if (region.State == MapRegionState.Ready)
                {
                    region.SendFullPartUpdates(character);
                    pendingUpdate?.SentRegions.Add(region);
                }
            }

            if (initializingRegions.Length == 0 || pendingUpdate is not null)
            {
                return;
            }

            var nextPendingUpdate = new PendingRegionUpdate(initializingRegions);
            _pendingRegionUpdates.Add(character, nextPendingUpdate);
            character.QueueTask(async cancellationToken =>
            {
                try
                {
                    await _regionLoadScheduler.EnsureLoadedAsync(initializingRegions, cancellationToken);
                    if (!ReferenceEquals(_characterStore.FindByMasterId(character.MasterId), character)
                        || character.Viewport.ShouldRebuild())
                    {
                        return;
                    }

                    foreach (var requestedRegion in initializingRegions)
                    {
                        var currentRegion = _regionService.FindMapRegion(requestedRegion.Id, requestedRegion.BaseLocation.Dimension);
                        if (currentRegion is null || currentRegion.State != MapRegionState.Ready
                            || !character.Viewport.VisibleRegions.Any(region =>
                                region.Id == currentRegion.Id && region.BaseLocation.Dimension == currentRegion.BaseLocation.Dimension)
                            || nextPendingUpdate.SentRegions.Contains(currentRegion))
                        {
                            continue;
                        }

                        currentRegion.SendFullPartUpdates(character);
                        nextPendingUpdate.SentRegions.Add(currentRegion);
                    }
                }
                finally
                {
                    _pendingRegionUpdates.Remove(character);
                }
            });
        }
    }
}
