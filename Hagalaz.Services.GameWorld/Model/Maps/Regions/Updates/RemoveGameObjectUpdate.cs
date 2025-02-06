using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class RemoveGameObjectUpdate : IRegionPartUpdate
    {
        public IGameObject GameObject { get; }
        public ILocation Location => GameObject.Location;

        public RemoveGameObjectUpdate(IGameObject obj) => GameObject = obj;

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location);

        public void OnUpdatedFor(ICharacter character) => GameObject.Script.OnDeletedFor(character);
    }
}