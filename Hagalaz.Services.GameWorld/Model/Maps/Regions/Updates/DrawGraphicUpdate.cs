using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class DrawGraphicUpdate : IRegionPartUpdate
    {
        public IGraphic Graphic { get; }
        public ILocation Location { get; }

        public DrawGraphicUpdate(IGraphic graphic, ILocation location)
        {
            Graphic = graphic;
            Location = location;
        }

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location);

        public void OnUpdatedFor(ICharacter character)
        {
        }
    }
}