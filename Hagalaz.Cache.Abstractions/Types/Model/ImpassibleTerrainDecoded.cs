namespace Hagalaz.Cache.Abstractions.Types.Model
{
    /// <summary>
    /// Represents the callback method that is invoked when impassable terrain data is decoded from the cache.
    /// This delegate is typically used by a map decoder to notify about a tile that cannot be traversed.
    /// </summary>
    /// <param name="localX">The local x-coordinate of the impassable tile within a map region.</param>
    /// <param name="localY">The local y-coordinate of the impassable tile within a map region.</param>
    /// <param name="z">The plane or height level (z-coordinate) of the impassable tile.</param>
    public delegate void ImpassibleTerrainDecoded(int localX, int localY, int z);
}
