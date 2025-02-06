using System.Drawing;
using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class AreaService : IAreaService
    {
        private readonly AreaStore _areaStore;

        public AreaService(AreaStore areaStore)
        {
            _areaStore = areaStore;
        }

        public IArea FindAreaByLocation(ILocation location) =>
            _areaStore.Areas.GetObjects(new Rectangle(location.X, location.Y, 1, 1)).LastOrDefault(_areaStore.DefaultArea);
    }
}