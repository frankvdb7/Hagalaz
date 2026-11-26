using System.Buffers;
using System.Numerics;
using System.Security.Cryptography;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Security;
using Hagalaz.Utilities;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Decoders
{
    public static class HandshakeDecoderHelper
    {
        internal delegate bool XteaPayloadParser(in ReadOnlySequence<byte> payload);

        internal delegate bool SignInPayloadParser(ref SequenceReader<byte> reader, uint[] isaacSeed, string password, string login);
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

                if (!rsaDataReader.TryReadBigEndian(out long _) || !rsaDataReader.TryReadBigEndian(out long _))
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

            const int xteaBlockSize = 8;
            var payloadSize = reader.Remaining;
            if (payloadSize < xteaBlockSize || payloadSize > int.MaxValue)
            {
                return false;
            }

            var length = (int)payloadSize;
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

        /// <summary>
        /// Parses the RSA header and the RSA-protected block that both sign-in packets carry: the ISAAC seed and the password.
        /// </summary>
        /// <returns><c>true</c> if both blocks were valid; otherwise <c>false</c>.</returns>
        public static bool TryParseRsa(ref SequenceReader<byte> reader, BigInteger privateKey, BigInteger modulusKey, out uint[] isaacSeed, out string password)
        {
            if (TryParseRsaHeader(ref reader, privateKey, modulusKey, out var rsaBigInteger) && TryParseRsaBlock(rsaBigInteger, out isaacSeed, out password))
            {
                return true;
            }

            isaacSeed = default!;
            password = default!;
            return false;
        }

        /// <summary>
        /// Parses what follows the packet header of a sign-in packet: the RSA-protected block (ISAAC seed and password)
        /// and then the XTEA-encrypted payload. The payload starts with the login; <paramref name="parser"/> reads the remaining fields.
        /// </summary>
        /// <returns><c>true</c> if the RSA block was valid and <paramref name="parser"/> accepted the payload.</returns>
        internal static bool TryParseSignInPayload(ref SequenceReader<byte> reader, BigInteger privateKey, BigInteger modulusKey, SignInPayloadParser parser)
        {
            if (!TryParseRsa(ref reader, privateKey, modulusKey, out var isaacSeed, out var password))
            {
                return false;
            }

            return TryParseXteaBlock(ref reader, isaacSeed, (in ReadOnlySequence<byte> xteaData) =>
            {
                var xteaDataReader = new SequenceReader<byte>(xteaData);
                return TryReadLogin(ref xteaDataReader, out var login) && parser(ref xteaDataReader, isaacSeed, password, login);
            });
        }

        /// <summary>
        /// Reads the login name, sent either as an encoded 64-bit value or as a string.
        /// </summary>
        public static bool TryReadLogin(ref SequenceReader<byte> reader, out string login)
        {
            login = default!;
            if (!reader.TryRead(out bool isLoginString))
            {
                return false;
            }

            string? value;
            if (!isLoginString)
            {
                if (!reader.TryReadBigEndian(out long encodedLogin))
                {
                    return false;
                }

                value = encodedLogin.LongToString();
            }
            else if (!reader.TryRead(out value))
            {
                return false;
            }

            if (value == null)
            {
                return false;
            }

            login = value;
            return true;
        }

        /// <summary>
        /// Reads the fixed 24-byte client (user) id.
        /// </summary>
        public static bool TryReadUserId(ref SequenceReader<byte> reader, out byte[] userId)
        {
            userId = new byte[24];
            if (!reader.TryCopyTo(userId))
            {
                return false;
            }

            reader.Advance(userId.Length);
            return true;
        }

        /// <summary>
        /// Reads the settings data length byte and skips that many bytes.
        /// </summary>
        public static bool TrySkipSettingsData(ref SequenceReader<byte> reader)
        {
            // TODO - read settings block
            if (!reader.TryRead(out byte settingsDataLength) || reader.Remaining < settingsDataLength)
            {
                return false;
            }

            reader.Advance(settingsDataLength);
            return true;
        }

        /// <summary>
        /// Reads the cache index CRCs, one per cache index (the master index itself is excluded).
        /// </summary>
        public static bool TryReadCacheCrcs(ref SequenceReader<byte> reader, ICacheAPI cacheApi, out int[] cacheCrcs)
        {
            var cacheCrcCount = cacheApi.GetFileCount(byte.MaxValue) - 1;
            if (cacheCrcCount < 0)
            {
                cacheCrcs = default!;
                return false;
            }

            cacheCrcs = new int[cacheCrcCount];
            for (var indexId = 0; indexId < cacheCrcs.Length; indexId++)
            {
                if (!reader.TryReadBigEndian(out int crc))
                {
                    return false;
                }

                cacheCrcs[indexId] = crc;
            }

            return true;
        }

        public static bool TryParseHardwareBlock(ref SequenceReader<byte> reader)
        {
            if (!reader.TryRead(out byte hardwareMagic) || hardwareMagic != 6)
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryRead(out bool _))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryRead(out bool _))
            {
                return false;
            }

            if (!reader.TryReadBigEndian(out short _))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (reader.Remaining < 3)
            {
                return false;
            }
            reader.Advance(3); // medium int cpu data

            if (!reader.TryReadBigEndian(out short _))
            {
                return false;
            }

            if (!reader.TryRead(out string _, true))
            {
                return false;
            }


            if (!reader.TryRead(out string _, true))
            {
                return false;
            }

            if (!reader.TryRead(out string _, true))
            {
                return false;
            }

            if (!reader.TryRead(out string _, true))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryReadBigEndian(out short _))
            {
                return false;
            }

            if (!reader.TryRead(out string _, true))
            {
                return false;
            }

            if (!reader.TryRead(out string _, true))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
            {
                return false;
            }

            if (!reader.TryRead(out byte _))
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

            if (!reader.TryReadBigEndian(out int _))
            {
                return false;
            }
            return true;
        }
    }
}
