using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents a decoded map, including terrain and object data.
    /// </summary>
    public class MapType : IMapType
    {
        /// <inheritdoc />
        public int Id { get; internal set; }

        /// <inheritdoc />
        public sbyte[,,] TerrainData { get; internal set; } = new sbyte[4, 64, 64];

        /// <inheritdoc />
        public IReadOnlyList<IMapObject> Objects => InternalObjects;

        /// <summary>
        /// Gets the internal list of map objects.
        /// </summary>
        internal List<MapObject> InternalObjects { get; } = new List<MapObject>();
    }

    /// <summary>
    /// Represents an object on a map.
    /// </summary>
    public class MapObject : IMapObject
    {
        /// <inheritdoc />
        public int Id { get; internal set; }

        /// <inheritdoc />
        public int ShapeType { get; internal set; }

        /// <inheritdoc />
        public int Rotation { get; internal set; }

        /// <inheritdoc />
        public int X { get; internal set; }

        /// <inheritdoc />
        public int Y { get; internal set; }

        /// <inheritdoc />
        public int Z { get; internal set; }
    }
}
