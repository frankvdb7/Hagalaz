using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Services.GameWorld.Builders
{
    /// <summary>
    /// 
    /// </summary>
    public class LocationBuilder : ILocationBuilder, ILocationX, ILocationY, ILocationOptional, ILocationBuild
    {
        private int _x;
        private int _y;
        private int _z;
        private int _dimension;

        public ILocationX Create() => new LocationBuilder();

        public ILocationOptional FromLocation(ILocation location)
        {
            _x = location.X;
            _y = location.Y;
            _z = location.Z;
            _dimension = location.Dimension;
            return this;
        }

        public ILocationY WithX(int x)
        {
            _x = x;
            return this;
        }

        public ILocationOptional WithY(int y)
        {
            _y = y;
            return this;
        }

        public ILocation Build() => new Location(_x, _y, _z, _dimension);

        public ILocationOptional WithZ(int z)
        {
            _z = z;
            return this;
        }

        public ILocationOptional WithDimension(int dimension)
        {
            _dimension = dimension;
            return this;
        }

        public ILocationOptional FromRegionId(int regionId)
        {
            var regionX = regionId >> 8;
            var regionY = regionId & 0xFF;
            _x = regionX << 6;
            _y = regionY << 6;
            return this;
        }

        public ILocationBuild ToRegionCoordinates(int localX, int localY, int regionSizeX, int regionSizeY)
        {
            var regionX = _x >> 6;
            var regionY = _y >> 6;
            _x = LocationHelper.ConvertLocalToAbsolute(localX, regionX, regionSizeX);
            _y = LocationHelper.ConvertLocalToAbsolute(localY, regionY, regionSizeY);
            return this;
        }
    }
}