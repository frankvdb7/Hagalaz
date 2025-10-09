using Hagalaz.Cache.Abstractions.Types.Model;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a contract for a map decoder, which is responsible for parsing map region data from the cache.
    /// This includes decoding terrain, objects, and other map-related features.
    /// </summary>
    public interface IMapDecoder
    {
        /// <summary>
        /// Decodes the map data for a specific region and invokes callbacks for decoded objects and impassable terrain.
        /// </summary>
        /// <param name="regionId">The unique identifier for the map region to decode.</param>
        /// <param name="xteaKeys">The set of XTEA decryption keys required to decrypt the map data for the region.</param>
        /// <param name="objectDecoded">
        /// A callback delegate (<see cref="ObjectDecoded"/>) that is invoked for each game object decoded from the map data.
        /// </param>
        /// <param name="impassableTerrainDecoded">
        /// A callback delegate (<see cref="ImpassibleTerrainDecoded"/>) that is invoked for each tile that is marked as impassable.
        /// </param>
        public void Decode(int regionId, int[] xteaKeys, ObjectDecoded objectDecoded, ImpassibleTerrainDecoded impassableTerrainDecoded);
    }
}
