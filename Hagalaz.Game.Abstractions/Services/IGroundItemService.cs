using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Services
{
    public interface IGroundItemService
    {
        public IEnumerable<IGroundItem> FindByLocation(ILocation location);
    }
}
