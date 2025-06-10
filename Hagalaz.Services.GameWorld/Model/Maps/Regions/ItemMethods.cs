using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Services.GameWorld.Model.Items;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Contains methods for editing ground items.
    /// </summary>
    public partial class MapRegion
    {
        public IEnumerable<IGroundItem> FindAllGroundItems() => _parts.SelectMany(part => part.FindAllGroundItems());

        public void Add(IGroundItem item) => _parts
            .GetOrAdd(item.Location.GetRegionPartHash(), CreateRegionPart)
            .Add(item);

        public void Remove(IGroundItem item)
        {
            var partHash = item.Location.GetRegionPartHash();
            if (!_parts.TryGetValue(partHash, out var part))
            {
                return;
            }
            part.Remove(item);
        }

        private void TickGroundItems()
        {
            foreach (var groundItem in FindAllGroundItems().ToArray())
            {
                groundItem.TicksLeft--;

                if (groundItem.TicksLeft > 0)
                    continue;

                var partHash = groundItem.Location.GetRegionPartHash();
                if (_parts.TryGetValue(partHash, out var part))
                {
                    part.ProcessExpiredItem(groundItem);
                }
            }
        }
    }
}