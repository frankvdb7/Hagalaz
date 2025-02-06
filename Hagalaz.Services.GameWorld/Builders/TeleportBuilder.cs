using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Teleport;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class TeleportBuilder : ITeleportBuilder, ITeleportType, ITeleportY, ITeleportBuild, ITeleportOptional
    {
        private readonly ILocationBuilder _locationBuilder;
        private int _x;
        private int _y;
        private int _z;
        private int _dimension;

        public TeleportBuilder(ILocationBuilder locationBuilder) => _locationBuilder = locationBuilder;

        public ITeleportOptional WithLocation(ILocation location)
        {
            _x = location.X;
            _y = location.Y;
            _z = location.Z;
            _dimension = location.Dimension;
            return this;
        }

        public ITeleportY WithX(int x)
        {
            _x = x;
            return this;
        }

        public ITeleportOptional WithY(int y)
        {
            _y = y;
            return this;
        }

        public ITeleport Build() => Teleport.Create(_locationBuilder.Create().WithX(_x).WithY(_y).WithZ(_z).WithDimension(_dimension).Build());

        public ITeleportOptional WithZ(int z)
        {
            _z = z;
            return this;
        }

        public ITeleportOptional WithDimension(int dimension)
        {
            _dimension = dimension;
            return this;
        }

        public ITeleportType Create() => new TeleportBuilder(_locationBuilder);
    }
}