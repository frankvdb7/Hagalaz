using System.Threading;

namespace Hagalaz.Cache.Abstractions.Types.Providers
{
    public delegate void ObjectDecoded(int id, int type, int rotation, int localX, int localY, int z);
    public delegate void ImpassibleTerrainDecoded(int localX, int localY, int z);
    public delegate int CalculateObjectPartRotation(int objectId, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY);

    public class DecodePartRequest
    {
        public int RegionID { get; set; }
        public int[] XteaKeys { get; set; } = [];
        public int MinX { get; set; }
        public int MinY { get; set; }
        public int MaxX { get; set; }
        public int MaxY { get; set; }
        public int PartZ { get; set; }
        public int PartRotation { get; set; }
        public CalculateObjectPartRotation PartRotationCallback { get; set; } = null!;
        public ObjectDecoded Callback { get; set; } = null!;
        public ImpassibleTerrainDecoded GroundCallback { get; set; } = null!;
    }

    public interface IMapProvider : ITypeProvider<IMapType>
    {
        /// <summary>
        /// Gets a map definition, optionally decrypting its object data with XTEA keys.
        /// </summary>
        /// <param name="typeId">The map region identifier.</param>
        /// <param name="xteaKeys">Four XTEA keys, or null/an empty array to read unencrypted object data.</param>
        /// <returns>The decoded map definition.</returns>
        IMapType Get(int typeId, int[]? xteaKeys = null);
        DecodedMapRegion DecodeRegion(int regionId, int[] xteaKeys, CancellationToken cancellationToken);
        void DecodePart(DecodePartRequest request);
    }
}
