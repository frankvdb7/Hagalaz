namespace Hagalaz.Cache.Abstractions.Types.Model
{
    /// <summary>
    /// Callback when map object is decoded.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="shapeType">Type of the shape.</param>
    /// <param name="rotation">The rotation.</param>
    /// <param name="localX">The local X.</param>
    /// <param name="localY">The local Y.</param>
    /// <param name="z">The z.</param>
    public delegate void ObjectDecoded(int id, int shapeType, int rotation, int localX, int localY, int z);
}
