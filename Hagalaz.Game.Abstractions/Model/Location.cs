using System;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents a model's location in the game world.
    /// </summary>
    public readonly struct Location : ILocation, IVector2, IVector3, IEquatable<Location>
    {
        /// <summary>
        /// Contains dimension of this location.
        /// </summary>
        /// <value>The dimension.</value>
        public int Dimension { get; }

        /// <summary>
        /// Gets or sets the model's X coordinate.
        /// </summary>
        /// <value>The X.</value>
        public int X { get; }

        /// <summary>
        /// Gets or sets the model's Y coordinate.
        /// </summary>
        /// <value>The Y.</value>
        public int Y { get; }

        /// <summary>
        /// Gets or sets the model's Z coordinate.
        /// </summary>
        /// <value>The Z.</value>
        public int Z { get; }

        /// <summary>
        /// Gets the 8x8 splice X coordinate.
        /// </summary>
        /// <value>The region part X.</value>
        public int RegionPartX => X >> 3;

        /// <summary>
        /// Gets the 8x8 splice Y coordinate.
        /// </summary>
        /// <value>The region part Y.</value>
        public int RegionPartY => Y >> 3;

        /// <summary>
        /// Get's the 64x64 splice X of this location.
        /// </summary>
        /// <value>The region X.</value>
        public int RegionX => X >> 6;

        /// <summary>
        /// Get's the 64x64 splice X of this location.
        /// </summary>
        /// <value>The region Y.</value>
        public int RegionY => Y >> 6;

        /// <summary>
        /// Return's local X coordinate in the location's region.
        /// </summary>
        /// <value>The region local X.</value>
        public int RegionLocalX => X & 0x3F;

        /// <summary>
        /// Return's local Y coordinate in the location's region.
        /// </summary>
        /// <value>The region local Y.</value>
        public int RegionLocalY => Y & 0x3F;

        /// <summary>
        /// Get's the region Id of this location.
        /// </summary>
        /// <value>The region Id.</value>
        public int RegionId => (RegionX << 8) + RegionY;

        /// <summary>
        /// Constructs a new location instance.
        /// </summary>
        /// <param name="x">The instance's X coordinate.</param>
        /// <param name="y">The instance's Y coordinate.</param>
        /// <param name="z">The instance's Z coordinate.</param>
        /// <param name="dimension">The dimension.</param>
        public Location(int x, int y, int z, int dimension)
        {
            X = x;
            Y = y;
            Z = z;
            Dimension = dimension;
        }
        /// <summary>
        /// Creates a location instance.
        /// </summary>
        /// <param name="x">The instance's X coordinate.</param>
        /// <param name="y">The instance's Y coordinate.</param>
        /// <param name="z">The instance's Z coordinate.</param>
        /// <returns></returns>
        public static Location Create(int x, int y) => Create(x, y, 0, 0);

        /// <summary>
        /// Creates a location instance.
        /// </summary>
        /// <param name="x">The instance's X coordinate.</param>
        /// <param name="y">The instance's Y coordinate.</param>
        /// <param name="z">The instance's Z coordinate.</param>
        /// <returns></returns>
        public static Location Create(int x, int y, int z) => Create(x, y, z, 0);

        /// <summary>
        /// Creates a location instance.
        /// </summary>
        /// <param name="x">The instance's X coordinate.</param>
        /// <param name="y">The instance's Y coordinate.</param>
        /// <param name="z">The instance's Z coordinate.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns></returns>
        public static Location Create(int x, int y, int z, int dimension) => new(x, y, z, dimension);

        /// <summary>
        /// Copies the current location instance.
        /// </summary>
        /// <returns></returns>
        public ILocation Clone() => new Location(X, Y, Z, Dimension);

        /// <summary>
        /// Copies the specified dimension.
        /// </summary>
        /// <param name="dimension">The dimension.</param>
        /// <returns></returns>
        public ILocation Copy(int dimension) => new Location(X, Y, Z, dimension);

        /// <summary>
        /// Translates the specified location.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        public ILocation Translate(int x, int y, int z) => new Location(X + x, Y + y, Z + z, Dimension);

        /// <summary>
        /// Gets the location's sortHash code.
        /// </summary>
        /// <returns>Returns an integer.</returns>
        /// 2 bits for Z
        /// 13 bits for X
        /// 13 bits for Y
        /// 4 bits for Dimension
        public override int GetHashCode() => ((Z & 0x3) << 30) | ((X & 0xFFF) << 17) | ((Y & 0xFFF) << 4) | (Dimension & 0x3F);

        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <param name="toX">To x.</param>
        /// <param name="toY">To y.</param>
        /// <returns></returns>
        public DirectionFlag GetDirection(int toX, int toY) => DirectionHelper.GetDirection(X, Y, toX, toY);

        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <param name="to">To.</param>
        /// <returns></returns>
        public DirectionFlag GetDirection(ILocation to) => DirectionHelper.GetDirection(X, Y, to.X, to.Y);

        /// <summary>
        /// Compares two Location objects to see if their coordinates match.
        /// </summary>
        /// <param name="obj">The location object.</param>
        /// <returns>Returns true if both objects have the same coordinates; false if they dont.</returns>
        public override bool Equals(object? obj) => obj is Location location && Equals(location);

        public bool Equals(Location other) => Dimension == other.Dimension && X == other.X && Y == other.Y && Z == other.Z;

        /// <summary>
        /// Provides a string that shows the location's coordinates.
        /// </summary>
        /// <returns>Returns a string containing locations coordinates.</returns>
        public override string ToString() => "x=" + X + ",y=" + Y + ",z=" + Z + ",dimension=" + Dimension;

        /// <summary>
        /// Measures the distance between two locations.
        /// </summary>
        /// <param name="other">The location to compare with.</param>
        /// <param name="distance">The instance that the distance has to be within</param>
        /// <returns>Returns true if the given location is within distance of the given distance range; false if not.</returns>
        public bool WithinDistance(ILocation other, int distance)
        {
            if (other.Z != Z || other.Dimension != Dimension) return false;

            int deltaX = other.X - X, deltaY = other.Y - Y;
            return deltaX <= distance
                   && deltaX >= 0 - distance - 1
                   && deltaY <= distance
                   && deltaY >= 0 - distance - 1;
        }

        /// <summary>
        /// Access the /x/, /y/, /z/ or /dimension/ component using [0], [1], [2] or [3] respectively.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
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
        /// Location zero
        /// </summary>
        public static readonly Location Zero = new(0, 0, 0, 0);

        /// <summary>
        /// Location one
        /// </summary>
        public static readonly Location One = new(1, 1, 1, 0);

        /// <summary>
        /// Location up
        /// </summary>
        public static readonly Location Up = new(0, 1, 0, 0);

        /// <summary>
        /// Location down
        /// </summary>
        public static readonly Location Down = new(0, -1, 0, 0);

        /// <summary>
        /// Location left
        /// </summary>
        public static readonly Location Left = new(-1, 0, 0, 0);

        /// <summary>
        /// Location right
        /// </summary>
        public static readonly Location Right = new(1, 0, 0, 0);

        /// <summary>
        /// Gets the absolute distance between the specified location.
        /// </summary>
        /// <param name="other">The location to compare with.</param>
        /// <returns>Returns a double holding the value of the distance.</returns>
        public double GetDistance(ILocation other)
        {
            var xDiff = X - other.X;
            var yDiff = Y - other.Y;
            return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        /// <summary>
        /// Gets the absolute distance between the specified coordinates.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="y1">The y1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="y2">The y2.</param>
        /// <returns>Returns a double holding the value of the distance.</returns>
        public static double GetDistance(int x1, int y1, int x2, int y2)
        {
            var xDiff = x1 - x2;
            var yDiff = y1 - y2;
            return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        /// <summary>
        /// Gets delta location.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>Location.</returns>
        public static Location GetDelta(ILocation from, ILocation to) => Create(to.X - from.X, to.Y - from.Y, to.Z - from.Z, to.Dimension - from.Dimension);

        public static bool operator ==(Location left, Location right) => left.Equals(right);

        public static bool operator !=(Location left, Location right) => !(left == right);

        public static Location operator +(Location a, Location b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.Dimension + b.Dimension);

        public static Location operator -(Location a, Location b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.Dimension - b.Dimension);

        public static Location operator -(Location a) => new(-a.X, -a.Y, -a.Z, a.Dimension);
    }
}