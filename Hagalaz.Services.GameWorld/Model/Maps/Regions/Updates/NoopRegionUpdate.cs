using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class NoopRegionUpdate : IRegionPartUpdate
    {
        public ILocation Location { get; }

        public NoopRegionUpdate(ILocation location) => Location = location;

        public bool CanUpdateFor(ICharacter character) => false;

        public void OnUpdatedFor(ICharacter character) {}
    }
}