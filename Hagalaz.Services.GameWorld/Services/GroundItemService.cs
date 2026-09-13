using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GroundItemService : IGroundItemService
    {
        private readonly IMapRegionService _regionService;

        public GroundItemService(IMapRegionService regionService)
        {
            _regionService = regionService;
        }

        public IEnumerable<IGroundItem> FindByLocation(ILocation location)
        {
            var region = _regionService.FindMapRegion(location.RegionId, location.Dimension);
            if (region is null)
            {
                yield break;
            }

            foreach (var groundItem in region.FindAllGroundItems().Where(item => item.Location.Equals(location)))
            {
                yield return groundItem;
            }
        }

        public IEnumerable<IGroundItem> FindAllGroundItems(ILocation location)
        {
            var region = _regionService.FindMapRegion(location.RegionId, location.Dimension);
            if (region is null)
            {
                yield break;
            }

            foreach (var groundItem in region.FindAllGroundItems())
            {
                yield return groundItem;
            }
        }

    }
}
