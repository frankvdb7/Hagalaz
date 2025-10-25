using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Providers
{
    public delegate void ObjectDecoded(int id, int type, int rotation, int localX, int localY, int z);
    public delegate void ImpassibleTerrainDecoded(int localX, int localY, int z);
    public delegate int CalculateObjectPartRotation(int objectId, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY);

    public class DecodePartRequest
    {
        public int RegionID { get; set; }
        public int[] XteaKeys { get; set; }
        public int MinX { get; set; }
        public int MinY { get; set; }
        public int MaxX { get; set; }
        public int MaxY { get; set; }
        public int PartZ { get; set; }
        public int PartRotation { get; set; }
        public CalculateObjectPartRotation PartRotationCallback { get; set; }
        public ObjectDecoded Callback { get; set; }
        public ImpassibleTerrainDecoded GroundCallback { get; set; }
    }

    public interface IMapProvider : ITypeProvider<IMapType>
    {
        IMapType Get(int typeId, int[]? xteaKeys = null);
        void DecodePart(DecodePartRequest request);
    }
}
