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

        private sealed class PendingRegionUpdate
        {
            public HashSet<IMapRegion> InitializingRegions { get; } = new(ReferenceEqualityComparer.Instance);
            public HashSet<IMapRegion> SentRegions { get; } = new(ReferenceEqualityComparer.Instance);
            public bool IsScheduled { get; set; }
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

            _pendingRegionUpdates.TryGetValue(character, out var pendingUpdate);

            foreach (var region in visibleRegions)
            {
                if (region.State == MapRegionState.Initializing)
                {
                    _regionLoadScheduler.RequestLoad(region);
                    pendingUpdate ??= AddPendingRegionUpdate(character);
                    pendingUpdate.InitializingRegions.Add(region);
                }
                else if (region.State == MapRegionState.Ready)
                {
                    region.SendFullPartUpdates(character);
                    if (pendingUpdate is not null)
                    {
                        pendingUpdate.InitializingRegions.Remove(region);
                        pendingUpdate.SentRegions.Add(region);
                    }
                }
            }

            if (pendingUpdate is null || pendingUpdate.InitializingRegions.Count == 0)
            {
                return;
            }

            if (pendingUpdate.IsScheduled)
            {
                return;
            }

            pendingUpdate.IsScheduled = true;
            character.QueueTask(async cancellationToken =>
            {
                try
                {
                    while (pendingUpdate.InitializingRegions.Count > 0)
                    {
                        var requestedRegions = pendingUpdate.InitializingRegions.ToArray();
                        await _regionLoadScheduler.EnsureLoadedAsync(requestedRegions, cancellationToken);
                        if (!ReferenceEquals(_characterStore.FindByMasterId(character.MasterId), character)
                            || character.Viewport.ShouldRebuild())
                        {
                            return;
                        }

                        foreach (var requestedRegion in requestedRegions)
                        {
                            pendingUpdate.InitializingRegions.Remove(requestedRegion);
                            var currentRegion = _regionService.FindMapRegion(requestedRegion.Id, requestedRegion.BaseLocation.Dimension);
                            if (currentRegion is null || !character.Viewport.VisibleRegions.Any(region =>
                                    region.Id == currentRegion.Id
                                    && region.BaseLocation.Dimension == currentRegion.BaseLocation.Dimension))
                            {
                                continue;
                            }

                            if (currentRegion.State == MapRegionState.Initializing)
                            {
                                pendingUpdate.InitializingRegions.Add(currentRegion);
                                _regionLoadScheduler.RequestLoad(currentRegion);
                                continue;
                            }

                            if (currentRegion.State != MapRegionState.Ready
                                || pendingUpdate.SentRegions.Contains(currentRegion))
                            {
                                continue;
                            }

                            currentRegion.SendFullPartUpdates(character);
                            pendingUpdate.SentRegions.Add(currentRegion);
                        }
                    }
                }
                finally
                {
                    _pendingRegionUpdates.Remove(character);
                }
            });
        }

        private PendingRegionUpdate AddPendingRegionUpdate(ICharacter character)
        {
            var pendingUpdate = new PendingRegionUpdate();
            _pendingRegionUpdates.Add(character, pendingUpdate);
            return pendingUpdate;
        }
    }
}
