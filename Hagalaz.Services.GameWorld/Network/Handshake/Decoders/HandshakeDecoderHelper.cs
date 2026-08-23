using System;
using System.Buffers;
using System.Numerics;
using System.Security.Cryptography;
using Hagalaz.Security;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Decoders
{
    public static class HandshakeDecoderHelper
    {
        internal delegate bool XteaPayloadParser(in ReadOnlySequence<byte> payload);
        public static bool TryParsePacketHeader(ref SequenceReader<byte> reader, out int clientRevision, out int clientRevisionPatch)
        {
            if (!reader.TryReadBigEndian(out short packetSize) || packetSize < 0 || reader.Remaining != packetSize)
            {
                clientRevision = default;
                clientRevisionPatch = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out clientRevision))
            {
                clientRevisionPatch = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out clientRevisionPatch))
            {
                return false;
            }
            return true;
        }

        public static bool TryParseRsaHeader(ref SequenceReader<byte> reader, BigInteger privateKey, BigInteger modulusKey, out BigInteger rsaBigInteger)
        {
            if (!reader.TryReadBigEndian(out short rsaHeaderSize) || rsaHeaderSize <= 0 || reader.Remaining < rsaHeaderSize)
            {
                rsaBigInteger = default;
                return false;
            }
            var rsaBuffer = ArrayPool<byte>.Shared.Rent(rsaHeaderSize);
            try
            {
                var bufferSpan = rsaBuffer.AsSpan(0, rsaHeaderSize);
                if (!reader.TryCopyTo(bufferSpan))
                {
                    rsaBigInteger = default;
                    return false;
                }
                var value = new BigInteger(bufferSpan, false, true);
                rsaBigInteger = BigInteger.ModPow(value, privateKey, modulusKey);
                // Add sanity checks (e.g., ensure rsaBigInteger is non-negative)
                if (rsaBigInteger < 0 || rsaBigInteger >= modulusKey)
                {
                    return false;
                }
                reader.Advance(rsaHeaderSize);
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rsaBuffer.AsSpan(0, rsaHeaderSize));
                ArrayPool<byte>.Shared.Return(rsaBuffer, clearArray: true);
            }
        }

        public static bool TryParseRsaBlock(BigInteger rsaBigInteger, out uint[] isaacSeed, out string password)
        {
            var rsaData = ArrayPool<byte>.Shared.Rent(rsaBigInteger.GetByteCount());
            var rsaDataLength = 0;
            try
            {
                if (!rsaBigInteger.TryWriteBytes(rsaData, out rsaDataLength, false, true) || rsaDataLength <= 0)
                {
                    isaacSeed = default!;
                    password = default!;
                    return false;
                }

                var rsaDataReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(rsaData.AsMemory(0, rsaDataLength)));
                // check for fake packet or encryption
                if (!rsaDataReader.TryRead(out byte rsaMagic) || rsaMagic != 10)
                {
                    isaacSeed = default!;
                    password = default!;
                    return false;
                }
                isaacSeed = new uint[4];
                for (var i = 0; i < isaacSeed.Length; i++)
                {
                    if (!rsaDataReader.TryReadBigEndian(out int isaacKey))
                    {
                        isaacSeed = default!;
                        password = default!;
                        return false;
                    }
                    isaacSeed[i] = (uint)isaacKey;
                }

                if (!rsaDataReader.TryReadBigEndian(out long vHash) || vHash != 0L)
                {
                    isaacSeed = default!;
                    password = default!;
                    return false;
                }

                if (!rsaDataReader.TryRead(out password))
                {
                    isaacSeed = default!;
                    password = default!;
                    return false;
                }

                if (!rsaDataReader.TryReadBigEndian(out long clientSeed1) || !rsaDataReader.TryReadBigEndian(out long clientSeed2))
                {
                    isaacSeed = default!;
                    password = default!;
                    return false;
                }
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rsaData.AsSpan(0, rsaDataLength));
                ArrayPool<byte>.Shared.Return(rsaData, clearArray: true);
            }
        }

        internal static bool TryParseXteaBlock(ref SequenceReader<byte> reader, uint[] isaacSeed, XteaPayloadParser parser)
            => TryParseXteaBlock(ref reader, isaacSeed, parser, ArrayPool<byte>.Shared);

        internal static bool TryParseXteaBlock(
            ref SequenceReader<byte> reader,
            uint[] isaacSeed,
            XteaPayloadParser parser,
            ArrayPool<byte> bufferPool)
        {
            ArgumentNullException.ThrowIfNull(isaacSeed);
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentNullException.ThrowIfNull(bufferPool);

            var xteaBlockSize = reader.Remaining;
            if (xteaBlockSize <= 0 || xteaBlockSize > int.MaxValue || xteaBlockSize % 8 != 0)
            {
                return false;
            }

            var length = (int)xteaBlockSize;
            var xteaData = bufferPool.Rent(length);
            try
            {
                reader.UnreadSequence.CopyTo(xteaData.AsSpan(0, length));
                XTEA.Decrypt(xteaData.AsSpan(0, length), xteaData.AsSpan(0, length), isaacSeed);

                return parser(new ReadOnlySequence<byte>(xteaData.AsMemory(0, length)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(xteaData.AsSpan(0, length));
                bufferPool.Return(xteaData, clearArray: true);
            }
        }

        public static bool TryParseHardwareBlock(ref SequenceReader<byte> reader)
        {
            if (!reader.TryRead(out byte hardwareMagic) || hardwareMagic != 6)
            {
                return false;
            }

            if (!reader.TryRead(out byte operatingSystemId))
            {
                return false;
            }

            if (!reader.TryRead(out bool is64Bit))
            {
                return false;
            }

            if (!reader.TryRead(out byte operatingSystemVersion))
            {
                return false;
            }

            if (!reader.TryRead(out byte javaVendorId))
            {
                return false;
            }

            if (!reader.TryRead(out byte javaMinorVersion))
            {
                return false;
            }

            if (!reader.TryRead(out byte javaPatchVersion))
            {
                return false;
            }

            if (!reader.TryRead(out byte javaMajorVersion))
            {
                return false;
            }

            if (!reader.TryRead(out bool unknownBool))
            {
                return false;
            }

            if (!reader.TryReadBigEndian(out short maxMemory))
            {
                return false;
            }

            if (!reader.TryRead(out byte processorCount))
            {
                return false;
            }

            if (reader.Remaining < 3)
            {
                return false;
            }
            reader.Advance(3); // medium int cpu data

            if (!reader.TryReadBigEndian(out short cpuData))
            {
                return false;
            }

            if (!reader.TryRead(out string gpuName, true))
            {
                return false;
            }


            if (!reader.TryRead(out string someHardwareData, true))
            {
                return false;
            }

            if (!reader.TryRead(out string directXRuntimeVersion, true))
            {
                return false;
            }

            if (!reader.TryRead(out string unknownHardwareString2, true))
            {
                return false;
            }

            if (!reader.TryRead(out byte gpuDriverDateDay))
            {
                return false;
            }

            if (!reader.TryReadBigEndian(out short gpuDriverDateYear))
            {
                return false;
            }

            if (!reader.TryRead(out string cpuManufacturer, true))
            {
                return false;
            }

            if (!reader.TryRead(out string cpuName, true))
            {
                return false;
            }

            if (!reader.TryRead(out byte cpuCoreCount))
            {
                return false;
            }

            if (!reader.TryRead(out byte someHardwareValue))
            {
                return false;
            }

            var javaVersions = new int[3];
            for (var i = 0; i < javaVersions.Length; i++)
            {
                if (!reader.TryReadBigEndian(out int javaData))
                {
                    return false;
                }
                javaVersions[i] = javaData;
            }

            if (!reader.TryReadBigEndian(out int someCpuData3))
            {
                return false;
            }
            return true;
        }
    }
}
