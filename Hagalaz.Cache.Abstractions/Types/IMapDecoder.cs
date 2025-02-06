using Hagalaz.Cache.Abstractions.Types.Model;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface IMapDecoder
    {
        /// <summary>
        /// Decodes all objects for given region.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="regionId">The regionid.</param>
        /// <param name="xteaKeys">The xtea keys.</param>
        /// <param name="objectDecoded">The callback.</param>
        /// <param name="impassableTerrainDecoded">The ground callback.</param>
        public void Decode(int regionId, int[] xteaKeys, ObjectDecoded objectDecoded, ImpassibleTerrainDecoded impassableTerrainDecoded);
    }
}
