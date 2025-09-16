using System.IO;
using System.Linq;
using System.Numerics;
using Hagalaz.Cache.Extensions;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ChecksumTableCodecTests
    {
        private readonly IChecksumTableCodec _codec;

        // Sample 1024-bit RSA public key (e, n) and private key (d, n)
        // In a real application, these would be loaded securely.
        private static readonly BigInteger Modulus = BigInteger.Parse("12641740294002876620574397613075414225655517449542435745121204908052324722912638830330758064988204117132896976520347480663451744580208271996434546810414552390847098963896190230171670067632639339429644973780648935612246585684673151154001037820499063788130176920544621739659667072752248297286646267491600237758552684184941446427986810580935048997892375828337122700090176816095203924855665005842492587036034671202902325600738091516081946830414962816318355223832098377730805213189409499398047828856211723403501266893772998991757420458221500159343089503587469615316650564424901884415171869010133029284578657839636204489287");
        private static readonly BigInteger PublicExponent = BigInteger.Parse("65537");
        private static readonly BigInteger PrivateExponent = BigInteger.Parse("2991025298668059338673216799492612920686245228994384983503202821372039415192690811344869837736043655343740185207203687003791488036692394610322628177095198885705404832906210624670673300100885081666784792765044820416001580139868194787584724544069128631135793877198603913745865658026707998500491890439366362309544204905210127571928272612452068645965588949307734609680857541465770001239658446669919129417189545078123810622184958585933887532491972718347227389837871316065757143145241392663042913210995410383754495033599461182825610956751206198180700820940496627371043541353605316341478490579236166407686003210253048069761");


        public ChecksumTableCodecTests()
        {
            _codec = new ChecksumTableCodec();
        }

        private static void WriteIntBigEndian(Stream stream, int value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        [Fact]
        public void Decode_WithoutWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            using var stream = new MemoryStream();
            // Entry 1
            WriteIntBigEndian(stream, 123); // CRC
            WriteIntBigEndian(stream, 456); // Version
            // Entry 2
            WriteIntBigEndian(stream, 789); // CRC
            WriteIntBigEndian(stream, 101); // Version
            stream.Position = 0;

            // Act
            var table = _codec.Decode(stream);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(2, table.Count);
            Assert.Equal(123, table.Entries[0].Crc32);
            Assert.Equal(456, table.Entries[0].Version);
            Assert.Equal(789, table.Entries[1].Crc32);
            Assert.Equal(101, table.Entries[1].Version);
        }

        [Fact]
        public void Decode_WithWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            using var stream = new MemoryStream();
            stream.WriteByte(1); // Number of entries
            // Entry 1
            WriteIntBigEndian(stream, 123); // CRC
            WriteIntBigEndian(stream, 456); // Version
            stream.Write(new byte[64]); // Whirlpool digest

            var dataToHash = stream.ToArray();
            var whirlpoolHash = Security.Whirlpool.GenerateDigest(dataToHash, 0, dataToHash.Length);

            var rsaBlock = new byte[65];
            rsaBlock[0] = 10; // Padding
            Buffer.BlockCopy(whirlpoolHash, 0, rsaBlock, 1, 64);

            var rsaBigInt = new BigInteger(rsaBlock, isUnsigned: false, isBigEndian: true);
            var encryptedBigInt = BigInteger.ModPow(rsaBigInt, PrivateExponent, Modulus);
            var encryptedBytes = encryptedBigInt.ToByteArray(isUnsigned: false, isBigEndian: true);

            using var finalStream = new MemoryStream();
            finalStream.Write(dataToHash);
            finalStream.Write(encryptedBytes);
            finalStream.Position = 0;

            // Act
            var table = _codec.Decode(finalStream, true, Modulus, PublicExponent);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(1, table.Count);
            Assert.Equal(123, table.Entries[0].Crc32);
            Assert.Equal(456, table.Entries[0].Version);
        }

        [Fact]
        public void Encode_WithoutWhirlpool_ShouldReturnCorrectData()
        {
            // Arrange
            var table = new ChecksumTable(1);
            table.SetEntry(0, new ChecksumTableEntry(123, 456, new byte[64]));

            // Act
            var stream = _codec.Encode(table);

            // Assert
            Assert.NotNull(stream);
            Assert.Equal(8, stream.Length);
            stream.Position = 0;
            Assert.Equal(123, stream.ReadInt());
            Assert.Equal(456, stream.ReadInt());
        }

        [Fact]
        public void Encode_WithWhirlpool_ShouldReturnCorrectData()
        {
            // Arrange
            var table = new ChecksumTable(1);
            var digest = new byte[64];
            for (int i = 0; i < digest.Length; i++)
            {
                digest[i] = (byte)i;
            }
            table.SetEntry(0, new ChecksumTableEntry(123, 456, digest));

            // Act
            var stream = _codec.Encode(table, true);

            // Assert
            Assert.NotNull(stream);
            // 1 byte for entry count, 8 for crc/version, 64 for digest, 65 for rsa block
            Assert.Equal(1 + 8 + 64 + 65, stream.Length);
            stream.Position = 0;
            Assert.Equal(1, stream.ReadByte()); // entry count
            Assert.Equal(123, stream.ReadInt());
            Assert.Equal(456, stream.ReadInt());
            var writtenDigest = new byte[64];
            stream.Read(writtenDigest, 0, 64);
            Assert.Equal(digest, writtenDigest);

            // Verify whirlpool hash
            stream.Position = 0;
            var dataToHash = new byte[1 + 8 + 64];
            stream.Read(dataToHash, 0, dataToHash.Length);
            var whirlpoolHash = Security.Whirlpool.GenerateDigest(dataToHash, 0, dataToHash.Length);

            Assert.Equal(10, stream.ReadByte()); // RSA block type
            var writtenWhirlpoolHash = new byte[64];
            stream.Read(writtenWhirlpoolHash, 0, 64);
            Assert.Equal(whirlpoolHash, writtenWhirlpoolHash);
        }

        [Fact]
        public void Encode_WithWhirlpoolAndRsa_ShouldReturnCorrectData()
        {
            // Arrange
            var table = new ChecksumTable(1);
            var digest = new byte[64];
            for (int i = 0; i < digest.Length; i++)
            {
                digest[i] = (byte)i;
            }
            table.SetEntry(0, new ChecksumTableEntry(123, 456, digest));

            // Act
            // Sign with the private key
            var stream = _codec.Encode(table, true, Modulus, PrivateExponent);

            // Verify with the public key
            var decodedTable = _codec.Decode(stream, true, Modulus, PublicExponent);

            // Assert
            Assert.NotNull(decodedTable);
            Assert.Equal(1, decodedTable.Count);
            Assert.Equal(123, decodedTable.Entries[0].Crc32);
            Assert.Equal(456, decodedTable.Entries[0].Version);
            Assert.Equal(digest, decodedTable.Entries[0].Digest);
        }
    }
}
