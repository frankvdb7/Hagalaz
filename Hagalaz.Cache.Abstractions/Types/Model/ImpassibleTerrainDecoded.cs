namespace Hagalaz.Cache.Abstractions.Types.Model
{
    /// <summary>
    /// Callback when impassable terrain is decoded.
    /// </summary>
    /// <param name="localX">The local X.</param>
    /// <param name="localY">The local Y.</param>
    /// <param name="z">The z.</param>
    public delegate void ImpassibleTerrainDecoded(int localX, int localY, int z);
}
