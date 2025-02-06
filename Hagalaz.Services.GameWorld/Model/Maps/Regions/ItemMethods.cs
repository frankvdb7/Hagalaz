using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Extensions;

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
                if (groundItem.CanRespawn())
                {
                    if (!groundItem.IsRespawning || --groundItem.TicksLeft > 0)
                    {
                        continue;
                    }
                    Remove(groundItem);
                    Add(groundItem.Clone());
                }
                else if (--groundItem.TicksLeft <= 0)
                {
                    Remove(groundItem);
                    if (!groundItem.IsPublic && groundItem.ItemOnGround.ItemScript.CanTradeItem(groundItem.ItemOnGround, groundItem.Owner))
                    {
                        var publicGroundItem = groundItem.Clone();
                        publicGroundItem.Owner = null;
                        Add(publicGroundItem);
                    }
                }
            }
        }
    }
}