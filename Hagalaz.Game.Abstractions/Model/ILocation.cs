using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILocation : IVector3
    {
        /// <summary>
        /// Get's the 64x64 splice X of this location.
        /// </summary>
        /// <value>The region X.</value>
        int RegionX { get; }
        /// <summary>
        /// Get's the 64x64 splice X of this location.
        /// </summary>
        /// <value>The region Y.</value>
        int RegionY { get; }
        /// <summary>
        /// Gets the 8x8 splice X coordinate.
        /// </summary>
        /// <value>The region part X.</value>
        int RegionPartX { get; }
        /// <summary>
        /// Gets the 8x8 splice Y coordinate.
        /// </summary>
        /// <value>The region part Y.</value>
        int RegionPartY { get; }
        /// <summary>
        /// Return's local X coordinate in the location's region.
        /// </summary>
        /// <value>The region local X.</value>
        int RegionLocalX { get; }
        /// <summary>
        /// Return's local Y coordinate in the location's region.
        /// </summary>
        /// <value>The region local Y.</value>
        int RegionLocalY { get; }
        /// <summary>
        /// Contains dimension of this location.
        /// </summary>
        /// <value>The dimension.</value>
        int Dimension { get; }
        /// <summary>
        /// Gets the region identifier.
        /// </summary>
        /// <value>
        /// The region identifier.
        /// </value>
        int RegionId { get; }
        /// <summary>
        /// Translates the specified location.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        ILocation Translate(int x, int y, int z);
        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <param name="toX">To x.</param>
        /// <param name="toY">To y.</param>
        /// <returns></returns>
        DirectionFlag GetDirection(int toX, int toY);
        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <param name="to">To.</param>
        /// <returns></returns>
        DirectionFlag GetDirection(ILocation to);
        /// <summary>
        /// Gets the distance.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        double GetDistance(ILocation other);
        /// <summary>
        /// Measures the distance between two locations.
        /// </summary>
        /// <param name="other">The location to compare with.</param>
        /// <param name="distance">The instance that the distance has to be within</param>
        /// <returns>Returns true if the given location is within distance of the given distance range; false if not.</returns>
        bool WithinDistance(ILocation other, int distance);
        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns></returns>
        ILocation Clone();
        /// <summary>
        /// Copies the specified new dimension.
        /// </summary>
        /// <param name="newDimension">The new dimension.</param>
        /// <returns></returns>
        ILocation Copy(int newDimension);
    }
}
