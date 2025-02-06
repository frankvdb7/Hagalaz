using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Model;
using Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class ProjectileBuilder : IProjectileBuilder, IProjectileId, IProjectileFrom, IProjectileTo, IProjectileDuration, IProjectileOptional, IProjectileBuild
    {
        private int _graphicId;
        private int _duration;
        private int _delay;
        private int _slope;
        private int _angle;
        private int _fromHeight;
        private int _toHeight;
        private bool _adjustFromFlyingHeight;
        private bool _adjustToFlyingHeight;
        private ICreature? _fromCreature;
        private ICreature? _toCreature;
        private ILocation _fromLocation = default!;
        private ILocation _toLocation = default!;
        private readonly IMapRegionService _regionService;

        public ProjectileBuilder(IMapRegionService regionService) => _regionService = regionService;

        public IProjectileId Create() => new ProjectileBuilder(_regionService);

        public IProjectileFrom WithGraphicId(int id)
        {
            _graphicId = id;
            return this;
        }

        public IProjectileTo FromCreature(ICreature creature)
        {
            _fromCreature = creature;
            _fromLocation = creature.Location;
            return this;
        }

        public IProjectileTo FromGameObject(IGameObject gameObject)
        {
            _fromLocation = gameObject.Location;
            return this;
        }

        public IProjectileTo FromGroundItem(IGroundItem groundItem)
        {
            _fromLocation = groundItem.Location;
            return this;
        }

        public IProjectileTo FromLocation(ILocation location)
        {
            _fromLocation = location;
            return this;
        }

        public IProjectileDuration ToCreature(ICreature creature)
        {
            _toCreature = creature;
            _toLocation = creature.Location;
            return this;
        }

        public IProjectileDuration ToGameObject(IGameObject gameObject)
        {
            _toLocation = gameObject.Location;
            return this;
        }

        public IProjectileDuration ToGroundItem(IGroundItem groundItem)
        {
            _toLocation = groundItem.Location;
            return this;
        }

        public IProjectileDuration ToLocation(ILocation location)
        {
            _toLocation = location;
            return this;
        }

        public IProjectileOptional WithDuration(int duration)
        {
            _duration = duration;
            return this;
        }

        public IProjectile Build() =>
            new Projectile(_graphicId)
            {
                Delay = _delay,
                Duration = _duration,
                Angle = _angle,
                Slope = _slope,
                FromHeight = _fromHeight,
                ToHeight = _toHeight,
                FromCreature = _fromCreature,
                ToCreature = _toCreature,
                FromLocation = _fromLocation,
                ToLocation = _toLocation,
                AdjustFromFlyingHeight = _adjustFromFlyingHeight,
                AdjustToFlyingHeight = _adjustToFlyingHeight
            };

        public void Send()
        {
            var region = _regionService.GetOrCreateMapRegion(_fromLocation.RegionId, _fromLocation.Dimension, true);
            var projectile = Build();
            region.QueueUpdate(new DrawProjectileUpdate(projectile));
        }

        public IProjectileOptional AdjustFromFlyingHeight()
        {
            _adjustFromFlyingHeight = true;
            return this;
        }

        public IProjectileOptional AdjustToFlyingHeight()
        {
            _adjustToFlyingHeight = true;
            return this;
        }

        public IProjectileOptional WithDelay(int delay)
        {
            _delay = delay;
            return this;
        }

        public IProjectileOptional WithSlope(int slope)
        {
            _slope = slope;
            return this;
        }

        public IProjectileOptional WithAngle(int angle)
        {
            _angle = angle;
            return this;
        }

        public IProjectileOptional WithFromHeight(int height)
        {
            _fromHeight = height;
            return this;
        }

        public IProjectileOptional WithToHeight(int height)
        {
            _toHeight = height;
            return this;
        }
    }
}