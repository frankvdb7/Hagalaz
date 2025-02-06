using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    /// <summary>
    /// Ground item type update.
    /// </summary>
    public class SetGroundItemCountUpdate : IRegionPartUpdate
    {
        public IGroundItem Item { get; }

        public int OldCount { get; }

        public ILocation Location => Item.Location;

        public SetGroundItemCountUpdate(IGroundItem item, int oldCount)
        {
            Item = item;
            OldCount = oldCount;
        }
        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location) && Item.ItemOnGround.ItemScript.CanDrawFor(Item, character);

        public void OnUpdatedFor(ICharacter character)
        {
        }
    }
}