using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Services.GameWorld.Model.Items;
using Hagalaz.Game.Configuration;

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

                if (groundItem.IsRespawning)
                {
                    Remove(groundItem);

                    var respawnedItem = _groundItemBuilder
                        .Create()
                        .WithItem(groundItem.ItemOnGround.Clone())
                        .WithLocation(groundItem.Location.Clone())
                        .WithRespawnTicks(groundItem.RespawnTicks)
                        .WithTicks(groundItem.RespawnTicks)
                        .Build();

                    Add(respawnedItem);

                    continue;
                }

                if (groundItem.CanRespawn())
                {
                    Remove(groundItem);
                    continue;
                }

                Remove(groundItem);
                if (!groundItem.IsPublic && groundItem.ItemOnGround.ItemScript.CanTradeItem(groundItem.ItemOnGround, groundItem.Owner))
                {
                    var publicGroundItem = _groundItemBuilder
                        .Create()
                        .WithItem(groundItem.ItemOnGround.Clone())
                        .WithLocation(groundItem.Location.Clone())
                        .WithRespawnTicks(0)
                        .Build();
                    Add(publicGroundItem);
                }
            }
        }
    }
}