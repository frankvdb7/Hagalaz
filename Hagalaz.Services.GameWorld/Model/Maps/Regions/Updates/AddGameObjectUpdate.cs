using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class AddGameObjectUpdate : IRegionPartUpdate, IEquatable<AddGameObjectUpdate>
    {
        public IGameObject GameObject { get; }

        public ILocation Location => GameObject.Location;

        public AddGameObjectUpdate(IGameObject obj) => GameObject = obj;

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location) && GameObject.Script.CanRenderFor(character);

        public void OnUpdatedFor(ICharacter character) => GameObject.Script.OnRenderedFor(character);

        public bool Equals(AddGameObjectUpdate? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetHashCode() == GetHashCode();
        }

        public override bool Equals(object? obj) => Equals(obj as AddGameObjectUpdate);

        public override int GetHashCode() => (GameObject, Location).GetHashCode();
    }
}