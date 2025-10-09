namespace Hagalaz.Cache.Abstractions.Types.Model
{
    /// <summary>
    /// Represents the callback method that is invoked when a map object is decoded from the cache.
    /// This delegate is used by a map decoder to notify about a decoded game object, providing its details.
    /// </summary>
    /// <param name="id">The unique identifier of the decoded object type.</param>
    /// <param name="shapeType">The shape type of the object, which defines its interaction and placement rules (e.g., wall, decoration).</param>
    /// <param name="rotation">The orientation or rotation of the object on the map (typically a value from 0 to 3).</param>
    /// <param name="localX">The local x-coordinate of the object's position within a map region.</param>
    /// <param name="localY">The local y-coordinate of the object's position within a map region.</param>
    /// <param name="z">The plane or height level (z-coordinate) where the object is located.</param>
    public delegate void ObjectDecoded(int id, int shapeType, int rotation, int localX, int localY, int z);
}
