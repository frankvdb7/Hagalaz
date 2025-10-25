namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents a decoded map, including terrain and object data.
    /// </summary>
    public interface IMapType : IType
    {
        /// <summary>
        /// Gets the terrain data for the map.
        /// </summary>
        sbyte[,,] TerrainData { get; }

        /// <summary>
        /// Gets the list of objects on the map.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<IMapObject> Objects { get; }
    }

    /// <summary>
    /// Represents an object on a map.
    /// </summary>
    public interface IMapObject
    {
        /// <summary>
        /// Gets the object's ID.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the object's shape type.
        /// </summary>
        int ShapeType { get; }

        /// <summary>
        /// Gets the object's rotation.
        /// </summary>
        int Rotation { get; }

        /// <summary>
        /// Gets the object's X coordinate.
        /// </summary>
        int X { get; }

        /// <summary>
        /// Gets the object's Y coordinate.
        /// </summary>
        int Y { get; }

        /// <summary>
        /// Gets the object's Z coordinate.
        /// </summary>
        int Z { get; }
    }
}
