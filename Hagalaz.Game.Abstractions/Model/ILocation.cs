using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a 3D location in the game world, including methods for calculations related to distance, direction, and regions.
    /// </summary>
    public interface ILocation : IVector3
    {
        /// <summary>
        /// Gets the X-coordinate of the 64x64 map region this location belongs to.
        /// </summary>
        int RegionX { get; }

        /// <summary>
        /// Gets the Y-coordinate of the 64x64 map region this location belongs to.
        /// </summary>
        int RegionY { get; }

        /// <summary>
        /// Gets the X-coordinate of the 8x8 map region part this location belongs to.
        /// </summary>
        int RegionPartX { get; }

        /// <summary>
        /// Gets the Y-coordinate of the 8x8 map region part this location belongs to.
        /// </summary>
        int RegionPartY { get; }

        /// <summary>
        /// Gets the local X-coordinate within the location's 64x64 map region.
        /// </summary>
        int RegionLocalX { get; }

        /// <summary>
        /// Gets the local Y-coordinate within the location's 64x64 map region.
        /// </summary>
        int RegionLocalY { get; }

        /// <summary>
        /// Gets the dimension (or instance) of this location, allowing for multiple parallel versions of the game world.
        /// </summary>
        int Dimension { get; }

        /// <summary>
        /// Gets the unique identifier for the 64x64 map region this location belongs to.
        /// </summary>
        int RegionId { get; }

        /// <summary>
        /// Creates a new location by translating (moving) this location by the specified offsets.
        /// </summary>
        /// <param name="x">The offset to apply to the X-coordinate.</param>
        /// <param name="y">The offset to apply to the Y-coordinate.</param>
        /// <param name="z">The offset to apply to the Z-coordinate (plane).</param>
        /// <returns>A new <see cref="ILocation"/> instance representing the translated location.</returns>
        ILocation Translate(int x, int y, int z);

        /// <summary>
        /// Calculates the direction from this location to a target coordinate.
        /// </summary>
        /// <param name="toX">The X-coordinate of the target location.</param>
        /// <param name="toY">The Y-coordinate of the target location.</param>
        /// <returns>A <see cref="DirectionFlag"/> representing the calculated direction.</returns>
        DirectionFlag GetDirection(int toX, int toY);

        /// <summary>
        /// Calculates the direction from this location to a target location.
        /// </summary>
        /// <param name="to">The target location.</param>
        /// <returns>A <see cref="DirectionFlag"/> representing the calculated direction.</returns>
        DirectionFlag GetDirection(ILocation to);

        /// <summary>
        /// Calculates the Euclidean distance between this location and another.
        /// </summary>
        /// <param name="other">The other location.</param>
        /// <returns>The distance as a double-precision floating-point number.</returns>
        double GetDistance(ILocation other);

        /// <summary>
        /// Checks if another location is within a specified Manhattan distance from this location.
        /// </summary>
        /// <param name="other">The location to compare with.</param>
        /// <param name="distance">The maximum distance to check for.</param>
        /// <returns><c>true</c> if the other location is within the specified distance; otherwise, <c>false</c>.</returns>
        bool WithinDistance(ILocation other, int distance);

        /// <summary>
        /// Creates a new, identical copy of this location.
        /// </summary>
        /// <returns>A new <see cref="ILocation"/> instance with the same coordinates and dimension.</returns>
        ILocation Clone();

        /// <summary>
        /// Creates a new copy of this location in a different dimension.
        /// </summary>
        /// <param name="newDimension">The dimension for the new location.</param>
        /// <returns>A new <see cref="ILocation"/> instance with the same coordinates but in the new dimension.</returns>
        ILocation Copy(int newDimension);
    }
}
