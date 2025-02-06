using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class RemoveGroundItemUpdate : IRegionPartUpdate
    {
        public IGroundItem Item { get; }

        public ILocation Location => Item.Location;

        public RemoveGroundItemUpdate(IGroundItem item) => Item = item;

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location);

        public void OnUpdatedFor(ICharacter character) => Item.ItemOnGround.ItemScript.OnDeletedFor(Item, character);
    }
}