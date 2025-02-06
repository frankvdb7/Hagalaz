using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    /// <summary>
    /// Object animation update.
    /// </summary>
    public class SetGameObjectAnimationUpdate : IRegionPartUpdate, IEquatable<SetGameObjectAnimationUpdate>
    {
        public ILocation Location => GameObject.Location;

        public IGameObject GameObject { get; }

        public IAnimation Animation { get; }

        public SetGameObjectAnimationUpdate(IGameObject obj, IAnimation animation)
        {
            GameObject = obj;
            Animation = animation;
        }

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Location) && GameObject.Script.CanRenderFor(character);

        public void OnUpdatedFor(ICharacter character)
        {

        }

        public bool Equals(SetGameObjectAnimationUpdate? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return GameObject.Equals(other.GameObject) && Animation.Equals(other.Animation);
        }

        public override bool Equals(object? obj) => Equals(obj as SetGameObjectAnimationUpdate);

        public override int GetHashCode() => (GameObject, Location, Animation).GetHashCode();
    }
}