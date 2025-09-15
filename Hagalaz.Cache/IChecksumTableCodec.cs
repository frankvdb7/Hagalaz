using System.IO;
using System.Numerics;

namespace Hagalaz.Cache
{
    public interface IChecksumTableCodec
    {
        ChecksumTable Decode(Stream stream, bool whirlpool = false, BigInteger? modulus = null, BigInteger? exponent = null);
        MemoryStream Encode(ChecksumTable table, bool whirlpool = false, BigInteger? modulus = null, BigInteger? exponent = null);
    }
}
