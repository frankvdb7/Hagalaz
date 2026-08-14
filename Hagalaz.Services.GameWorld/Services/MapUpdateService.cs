using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Orchestrates synchronous map updates at the character render boundary.
    /// </summary>
    public sealed class MapUpdateService : IMapUpdateService
    {
        private readonly IMapRegionService _regionService;

        public MapUpdateService(IMapRegionService regionService) => _regionService = regionService;

        public void UpdateMap(ICharacter character, bool forceUpdate, bool renderViewPort = false)
        {
            var viewport = character.Viewport;
            viewport.RebuildView();

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
                    VisibleRegionXteaKeys = viewport.VisibleRegions.Select(region => region.XteaKeys).ToList()
                });
            }

            foreach (var region in viewport.VisibleRegions)
            {
                _regionService.EnsureRegionLoadScheduled(region);
                region.SendFullPartUpdates(character);
            }
        }
    }
}
