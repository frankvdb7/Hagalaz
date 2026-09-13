using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Orchestrates synchronous map updates at the character render boundary.
    /// </summary>
    public sealed class MapUpdateService : IMapUpdateService
    {
        private readonly IMapRegionLoadScheduler _regionLoadScheduler;

        public MapUpdateService(IMapRegionLoadScheduler regionLoadScheduler) =>
            _regionLoadScheduler = regionLoadScheduler;

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

            foreach (var region in visibleRegions)
            {
                if (region.State == MapRegionState.Initializing)
                {
                    _regionLoadScheduler.RequestLoad(region);
                }
                else if (region.State == MapRegionState.Ready)
                {
                    region.SendFullPartUpdates(character);
                }
            }
        }
    }
}
