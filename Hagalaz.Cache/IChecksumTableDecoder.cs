using System.IO;
using System.Numerics;

namespace Hagalaz.Cache
{
    public interface IChecksumTableDecoder
    {
        ChecksumTable Decode(MemoryStream stream);
        ChecksumTable Decode(MemoryStream stream, bool whirlpool);
        ChecksumTable Decode(MemoryStream stream, bool whirlpool, BigInteger modulus, BigInteger publicKey);
    }
}
