using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates
{
    public class DrawProjectileUpdate : IRegionPartUpdate
    {
        public IProjectile Projectile { get; }

        public ILocation Location => Projectile.FromLocation;

        public DrawProjectileUpdate(IProjectile prj) => Projectile = prj;

        public bool CanUpdateFor(ICharacter character) => character.Viewport.InBounds(Projectile.FromLocation);

        public void OnUpdatedFor(ICharacter character)
        {
        }
    }
}