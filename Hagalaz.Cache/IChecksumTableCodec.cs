using System.IO;
using System.Numerics;

namespace Hagalaz.Cache
{
    public interface IChecksumTableCodec
    {
        MemoryStream Encode(ChecksumTable table);
        MemoryStream Encode(ChecksumTable table, bool whirlpool);
        MemoryStream Encode(ChecksumTable table, bool whirlpool, BigInteger modulus, BigInteger privateKey);

        ChecksumTable Decode(MemoryStream stream);
        ChecksumTable Decode(MemoryStream stream, bool whirlpool);
        ChecksumTable Decode(MemoryStream stream, bool whirlpool, BigInteger modulus, BigInteger publicKey);
    }
}
