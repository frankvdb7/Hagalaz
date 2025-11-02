using System;
using System.Buffers;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Security;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Decoders
{
    public class WorldHandshakeRequestDecoder : IRaidoMessageDecoder
    {
        private readonly IOptions<RsaClientConfig> _rsaOptions;
        private readonly ICacheAPI _cacheApi;

        public WorldHandshakeRequestDecoder(IOptions<RsaClientConfig> rsaOptions, ICacheAPI cacheApi)
        {
            _rsaOptions = rsaOptions;
            _cacheApi = cacheApi;
        }

        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!HandshakeDecoderHelper.TryParsePacketHeader(ref reader, out var clientRevision, out var clientRevisionPatch))
            {
                message = default;
                return false;
            }

            if (!reader.TryRead(out bool someLoginTypeBool))
            {
                message = default;
                return false;
            }

            var rsaKeys = _rsaOptions.Value;
            if (!HandshakeDecoderHelper.TryParseRsaHeader(ref reader, rsaKeys.PrivateKey, rsaKeys.ModulusKey, out var rsaBigInteger) ||
                !HandshakeDecoderHelper.TryParseRsaBlock(rsaBigInteger, out var isaacSeed, out var password))
            {
                message = default;
                return false;
            }

            // XTEA block
            var xteaBlockSize = (int)reader.Remaining;
            var xteaData = ArrayPool<byte>.Shared.Rent(xteaBlockSize);
            try
            {
                XTEA.Decrypt(reader.UnreadSpan, xteaData, isaacSeed);

                var xteaDataReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(xteaData));

                if (!xteaDataReader.TryRead(out bool isLoginString))
                {
                    message = default;
                    return false;
                }

                string? login;
                if (!isLoginString)
                {
                    if (!xteaDataReader.TryReadBigEndian(out long encodedLogin))
                    {
                        message = default;
                        return false;
                    }
                    login = encodedLogin.LongToString();
                }
                else
                {
                    if (!xteaDataReader.TryRead(out login))
                    {
                        message = default;
                        return false;
                    }
                }
                if (login == null)
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte displayMode))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out short screenSizeX))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out short screenSizeY))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte somePreferenceValue))
                {
                    message = default;
                    return false;
                }

                var userId = new byte[24];
                if (!xteaDataReader.TryCopyTo(userId))
                {
                    message = default;
                    return false;
                }
                xteaDataReader.Advance(24);

                if (!xteaDataReader.TryRead(out string settings))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out int affliateId))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte settingsDataLength))
                {
                    message = default;
                    return false;
                }

                // TODO - read settings block
                xteaDataReader.Advance(settingsDataLength);

                // start hardware block

                if (!HandshakeDecoderHelper.TryParseHardwareBlock(ref xteaDataReader))
                {
                    message = default;
                    return false;
                }

                // stop hardware block

                if (!xteaDataReader.TryReadBigEndian(out int somePacketIncrementalValue)) 
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out long someAppletLongSetting))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out string randomLoaderId))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out bool someClientSetting2))
                {
                    message = default;
                    return false;
                }

                if (someClientSetting2)
                {
                    if (!xteaDataReader.TryRead(out string clientSetting2))
                    {
                        message = default;
                        return false;
                    }
                }

                if (!xteaDataReader.TryRead(out bool jagTheoraLoaded))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out bool supportsJavascript))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out bool someRandomBool))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte afflicateId))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out int randomLoaderNumber))
                {
                    message = default;
                    return false;
                }

                if (!xteaDataReader.TryRead(out string serverToken))
                {
                    message = default;
                    return false;
                }
                if (!xteaDataReader.TryRead(out bool loggedInFromLobby))
                {
                    message = default;
                    return false;
                }

                // start cache CRC block
                var cacheCrCs = new int[_cacheApi.GetFileCount(byte.MaxValue) - 1];
                for (byte indexId = 0; indexId < cacheCrCs.Length; indexId++)
                {
                    if (!xteaDataReader.TryReadBigEndian(out int crc))
                    {
                        message = default;
                        return false;
                    }
                    cacheCrCs[indexId] = crc;
                }

                // stop cache CRC block

                message = new WorldSignInRequest
                {
                    ClientRevision = clientRevision,
                    ClientRevisionPatch = clientRevisionPatch,
                    Login = login,
                    Password = password,
                    IsaacSeed = isaacSeed,
                    CacheCRCs = cacheCrCs,
                    ClientId = Convert.ToHexString(userId),
                    DisplayMode = (DisplayMode)displayMode,
                    ClientSizeX = screenSizeX,
                    ClientSizeY = screenSizeY
                };
                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(xteaData);
            }
        }
    }
}
