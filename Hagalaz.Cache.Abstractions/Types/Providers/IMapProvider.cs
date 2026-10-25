using System.Threading;

namespace Hagalaz.Cache.Abstractions.Types.Providers
{
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
    }
}
