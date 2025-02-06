using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Abstractions.Model.Sound;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class SetTileSoundUpdate : IRegionPartUpdate
    {
        public ISound Sound { get; }

        public ILocation Location { get; }

        public SetTileSoundUpdate(ISound sound, ILocation location)
        {
            Sound = sound;
            Location = location;
        }

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location);

        public void OnUpdatedFor(ICharacter character)
        {
        }
    }
}