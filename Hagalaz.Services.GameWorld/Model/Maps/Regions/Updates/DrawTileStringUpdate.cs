using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class DrawTileStringUpdate : IRegionPartUpdate
    {
        public ILocation Location { get; }
        public string Value { get; }
        public int Duration { get; }
        public int Height { get; }
        public int Color { get; }

        public DrawTileStringUpdate(string value, int duration, int height, int color, ILocation location)
        {
            Value = value;
            Duration = duration;
            Height = height;
            Color = color;
            Location = location;
        }

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location);
        public void OnUpdatedFor(ICharacter character) {}
    }
}
