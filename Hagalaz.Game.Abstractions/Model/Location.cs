using System;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents an immutable 3D coordinate in the game world, including a dimension for instancing.
    /// </summary>
    public readonly struct Location : ILocation, IVector2, IVector3, IEquatable<Location>
    {
        /// <summary>
        /// Gets the dimension (or instance) of this location.
        /// </summary>
        public int Dimension { get; }

        /// <summary>
        /// Gets the X-coordinate of the location.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the Y-coordinate of the location.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Gets the Z-coordinate (or plane) of the location.
        /// </summary>
        public int Z { get; }

        /// <summary>
        /// Gets the X-coordinate of the 8x8 map region part this location belongs to.
        /// </summary>
        public int RegionPartX => X >> 3;

        /// <summary>
        /// Gets the Y-coordinate of the 8x8 map region part this location belongs to.
        /// </summary>
        public int RegionPartY => Y >> 3;

        /// <summary>
        /// Gets the X-coordinate of the 64x64 map region this location belongs to.
        /// </summary>
        public int RegionX => X >> 6;

        /// <summary>
        /// Gets the Y-coordinate of the 64x64 map region this location belongs to.
        /// </summary>
        public int RegionY => Y >> 6;

        /// <summary>
        /// Gets the local X-coordinate within the location's 64x64 map region.
        /// </summary>
        public int RegionLocalX => X & 0x3F;

        /// <summary>
        /// Gets the local Y-coordinate within the location's 64x64 map region.
        /// </summary>
        public int RegionLocalY => Y & 0x3F;

        /// <summary>
        /// Gets the unique identifier for the 64x64 map region this location belongs to.
        /// </summary>
        public int RegionId => (RegionX << 8) + RegionY;

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct.
        /// </summary>
        /// <param name="x">The X-coordinate.</param>
        /// <param name="y">The Y-coordinate.</param>
        /// <param name="z">The Z-coordinate (plane).</param>
        /// <param name="dimension">The dimension or instance ID.</param>
        public Location(int x, int y, int z, int dimension)
        {
            X = x;
            Y = y;
            Z = z;
            Dimension = dimension;
        }

        /// <summary>
        /// Creates a new <see cref="Location"/> instance on plane 0 and in dimension 0.
        /// </summary>
        /// <param name="x">The X-coordinate.</param>
        /// <param name="y">The Y-coordinate.</param>
        /// <returns>A new <see cref="Location"/> instance.</returns>
        public static Location Create(int x, int y) => Create(x, y, 0, 0);

        /// <summary>
        /// Creates a new <see cref="Location"/> instance in dimension 0.
        /// </summary>
        /// <param name="x">The X-coordinate.</param>
        /// <param name="y">The Y-coordinate.</param>
        /// <param name="z">The Z-coordinate (plane).</param>
        /// <returns>A new <see cref="Location"/> instance.</returns>
        public static Location Create(int x, int y, int z) => Create(x, y, z, 0);

        /// <summary>
        /// Creates a new <see cref="Location"/> instance.
        /// </summary>
        /// <param name="x">The X-coordinate.</param>
        /// <param name="y">The Y-coordinate.</param>
        /// <param name="z">The Z-coordinate (plane).</param>
        /// <param name="dimension">The dimension or instance ID.</param>
        /// <returns>A new <see cref="Location"/> instance.</returns>
        public static Location Create(int x, int y, int z, int dimension) => new(x, y, z, dimension);

        /// <summary>
        /// Creates a new, identical copy of this location.
        /// </summary>
        /// <returns>A new <see cref="ILocation"/> instance with the same coordinates and dimension.</returns>
        public ILocation Clone() => new Location(X, Y, Z, Dimension);

        /// <summary>
        /// Creates a new copy of this location in a different dimension.
        /// </summary>
        /// <param name="dimension">The dimension for the new location.</param>
        /// <returns>A new <see cref="ILocation"/> instance with the same coordinates but in the new dimension.</returns>
        public ILocation Copy(int dimension) => new Location(X, Y, Z, dimension);

        /// <summary>
        /// Creates a new location by translating (moving) this location by the specified offsets.
        /// </summary>
        /// <param name="x">The offset to apply to the X-coordinate.</param>
        /// <param name="y">The offset to apply to the Y-coordinate.</param>
        /// <param name="z">The offset to apply to the Z-coordinate (plane).</param>
        /// <returns>A new <see cref="ILocation"/> instance representing the translated location.</returns>
        public ILocation Translate(int x, int y, int z) => new Location(X + x, Y + y, Z + z, Dimension);

        /// <summary>
        /// Gets a hash code for the location, combining its coordinates and dimension into a single integer.
        /// </summary>
        /// <returns>An integer hash code.</returns>
        public override int GetHashCode() => ((Z & 0x3) << 30) | ((X & 0xFFF) << 17) | ((Y & 0xFFF) << 4) | (Dimension & 0x3F);

        /// <summary>
        /// Calculates the direction from this location to a target coordinate.
        /// </summary>
        /// <param name="toX">The X-coordinate of the target location.</param>
        /// <param name="toY">The Y-coordinate of the target location.</param>
        /// <returns>A <see cref="DirectionFlag"/> representing the calculated direction.</returns>
        public DirectionFlag GetDirection(int toX, int toY) => DirectionHelper.GetDirection(X, Y, toX, toY);

        /// <summary>
        /// Calculates the direction from this location to a target location.
        /// </summary>
        /// <param name="to">The target location.</param>
        /// <returns>A <see cref="DirectionFlag"/> representing the calculated direction.</returns>
        public DirectionFlag GetDirection(ILocation to) => DirectionHelper.GetDirection(X, Y, to.X, to.Y);

        /// <summary>
        /// Determines whether the specified object is equal to the current location.
        /// </summary>
        /// <param name="obj">The object to compare with the current location.</param>
        /// <returns><c>true</c> if the specified object is equal to the current location; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj) => obj is Location location && Equals(location);

        /// <summary>
        /// Determines whether another <see cref="Location"/> is equal to this one, comparing all coordinates and the dimension.
        /// </summary>
        /// <param name="other">The location to compare with this one.</param>
        /// <returns><c>true</c> if the locations are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(Location other) => Dimension == other.Dimension && X == other.X && Y == other.Y && Z == other.Z;

        /// <summary>
        /// Returns a string representation of the location's coordinates.
        /// </summary>
        /// <returns>A string in the format "x=X,y=Y,z=Z,dimension=Dimension".</returns>
        public override string ToString() => "x=" + X + ",y=" + Y + ",z=" + Z + ",dimension=" + Dimension;

        /// <summary>
        /// Checks if another location is within a specified Manhattan distance from this location on the same plane and dimension.
        /// </summary>
        /// <param name="other">The location to compare with.</param>
        /// <param name="distance">The maximum distance to check for.</param>
        /// <returns><c>true</c> if the other location is within the specified distance; otherwise, <c>false</c>.</returns>
        public bool WithinDistance(ILocation other, int distance)
        {
            if (other.Z != Z || other.Dimension != Dimension) return false;

            return Math.Abs(other.X - X) <= distance && Math.Abs(other.Y - Y) <= distance;
        }

        /// <summary>
        /// Gets the X, Y, Z, or Dimension component of the location by its index.
        /// </summary>
        /// <param name="index">The index of the component (0=X, 1=Y, 2=Z, 3=Dimension).</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is not between 0 and 3.</exception>
        public int this[int index] =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => Dimension,
                _ => throw new IndexOutOfRangeException($"Invalid Location index addressed: {index}!")
            };

        /// <summary>
        /// Represents a location at the origin (0, 0, 0) in dimension 0.
        /// </summary>
        public static readonly Location Zero = new(0, 0, 0, 0);

        /// <summary>
        /// Represents a location at (1, 1, 1) in dimension 0.
        /// </summary>
        public static readonly Location One = new(1, 1, 1, 0);

        /// <summary>
        /// Represents a unit vector pointing up (0, 1, 0).
        /// </summary>
        public static readonly Location Up = new(0, 1, 0, 0);

        /// <summary>
        /// Represents a unit vector pointing down (0, -1, 0).
        /// </summary>
        public static readonly Location Down = new(0, -1, 0, 0);

        /// <summary>
        /// Represents a unit vector pointing left (-1, 0, 0).
        /// </summary>
        public static readonly Location Left = new(-1, 0, 0, 0);

        /// <summary>
        /// Represents a unit vector pointing right (1, 0, 0).
        /// </summary>
        public static readonly Location Right = new(1, 0, 0, 0);

        /// <summary>
        /// Calculates the Euclidean distance between this location and another.
        /// </summary>
        /// <param name="other">The location to compare with.</param>
        /// <returns>The distance as a double-precision floating-point number.</returns>
        public double GetDistance(ILocation other)
        {
            var xDiff = X - other.X;
            var yDiff = Y - other.Y;
            return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        /// <summary>
        /// Calculates the Euclidean distance between two sets of coordinates.
        /// </summary>
        /// <param name="x1">The first X-coordinate.</param>
        /// <param name="y1">The first Y-coordinate.</param>
        /// <param name="x2">The second X-coordinate.</param>
        /// <param name="y2">The second Y-coordinate.</param>
        /// <returns>The distance as a double-precision floating-point number.</returns>
        public static double GetDistance(int x1, int y1, int x2, int y2)
        {
            var xDiff = x1 - x2;
            var yDiff = y1 - y2;
            return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        /// <summary>
        /// Calculates the delta (difference) between two locations.
        /// </summary>
        /// <param name="from">The starting location.</param>
        /// <param name="to">The ending location.</param>
        /// <returns>A new <see cref="Location"/> representing the vector from the start to the end location.</returns>
        public static Location GetDelta(ILocation from, ILocation to) => Create(to.X - from.X, to.Y - from.Y, to.Z - from.Z, to.Dimension - from.Dimension);

        public static bool operator ==(Location left, Location right) => left.Equals(right);

        public static bool operator !=(Location left, Location right) => !(left == right);

        public static Location operator +(Location a, Location b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.Dimension + b.Dimension);

        public static Location operator -(Location a, Location b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.Dimension - b.Dimension);

        public static Location operator -(Location a) => new(-a.X, -a.Y, -a.Z, a.Dimension);
    }
}