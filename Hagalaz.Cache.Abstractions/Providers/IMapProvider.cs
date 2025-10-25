using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Providers
{
    /// <summary>
    /// Callback for when an object is decoded from a map part.
    /// </summary>
    public delegate void ObjectDecoded(int id, int type, int rotation, int localX, int localY, int z);

    /// <summary>
    /// Callback for when impassable terrain is decoded from a map part.
    /// </summary>
    public delegate void ImpassibleTerrainDecoded(int localX, int localY, int z);

    /// <summary>
    /// Callback to calculate the rotation of an object part.
    /// </summary>
    public delegate int CalculateObjectPartRotation(int objectId, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY);

    /// <summary>
    /// Defines a contract for a provider that retrieves map data.
    /// </summary>
    public interface IMapProvider : ITypeProvider<IMapType>
    {
        /// <summary>
        /// Gets a map by its ID, optionally using XTEA keys for decryption.
        /// </summary>
        /// <param name="typeId">The ID of the map to retrieve.</param>
        /// <param name="xteaKeys">The XTEA keys for decryption, if required.</param>
        /// <returns>The decoded map data.</returns>
        IMapType Get(int typeId, int[]? xteaKeys = null);

        /// <summary>
        /// Decodes a specific part of a map, invoking callbacks for decoded objects and terrain.
        /// </summary>
        void DecodePart(
            int regionID,
            int[] xteaKeys,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int partZ,
            int partRotation,
            CalculateObjectPartRotation partRotationCallback,
            ObjectDecoded callback,
            ImpassibleTerrainDecoded groundCallback);
    }
}
