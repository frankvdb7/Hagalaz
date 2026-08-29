using System;
using System.Buffers;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
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
            message = default;
            var reader = new SequenceReader<byte>(input);
            if (!HandshakeDecoderHelper.TryParsePacketHeader(ref reader, out var clientRevision, out var clientRevisionPatch))
            {
                message = default;
                return false;
            }

            if (!reader.TryRead(out bool isReconnect))
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
            RaidoMessage? decodedMessage = default;
            var decoded = HandshakeDecoderHelper.TryParseXteaBlock(ref reader, isaacSeed,
                (in ReadOnlySequence<byte> xteaData) =>
                {
                var xteaDataReader = new SequenceReader<byte>(xteaData);

                if (!xteaDataReader.TryRead(out bool isLoginString))
                {
                    return false;
                }

                string? login;
                if (!isLoginString)
                {
                    if (!xteaDataReader.TryReadBigEndian(out long encodedLogin))
                    {
                        return false;
                    }
                    login = encodedLogin.LongToString();
                }
                else
                {
                    if (!xteaDataReader.TryRead(out login))
                    {
                        return false;
                    }
                }
                if (login == null)
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte displayMode))
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out short screenSizeX))
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out short screenSizeY))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte somePreferenceValue))
                {
                    return false;
                }

                var userId = new byte[24];
                if (!xteaDataReader.TryCopyTo(userId))
                {
                    return false;
                }
                xteaDataReader.Advance(24);

                if (!xteaDataReader.TryRead(out string settings))
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out int affliateId))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte settingsDataLength))
                {
                    return false;
                }

                // TODO - read settings block
                if (xteaDataReader.Remaining < settingsDataLength)
                {
                    return false;
                }
                xteaDataReader.Advance(settingsDataLength);

                // start hardware block

                if (!HandshakeDecoderHelper.TryParseHardwareBlock(ref xteaDataReader))
                {
                    return false;
                }

                // stop hardware block

                if (!xteaDataReader.TryReadBigEndian(out int somePacketIncrementalValue)) 
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out long someAppletLongSetting))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out string randomLoaderId))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out bool someClientSetting2))
                {
                    return false;
                }

                if (someClientSetting2)
                {
                    if (!xteaDataReader.TryRead(out string clientSetting2))
                    {
                        return false;
                    }
                }

                if (!xteaDataReader.TryRead(out bool jagTheoraLoaded))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out bool supportsJavascript))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out bool someRandomBool))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte afflicateId))
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out int randomLoaderNumber))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out string serverToken))
                {
                    return false;
                }
                if (!xteaDataReader.TryRead(out bool loggedInFromLobby))
                {
                    return false;
                }

                // start cache CRC block
                var cacheCrcCount = _cacheApi.GetFileCount(byte.MaxValue) - 1;
                if (cacheCrcCount < 0)
                {
                    return false;
                }
                var cacheCrCs = new int[cacheCrcCount];
                for (var indexId = 0; indexId < cacheCrCs.Length; indexId++)
                {
                    if (!xteaDataReader.TryReadBigEndian(out int crc))
                    {
                        return false;
                    }
                    cacheCrCs[indexId] = crc;
                }

                // stop cache CRC block

                if (isReconnect)
                {
                    decodedMessage = new WorldReconnectRequest
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
                }
                else
                {
                    decodedMessage = new WorldSignInRequest
                    {
                        ClientRevision = clientRevision,
                        ClientRevisionPatch = clientRevisionPatch,
                        Login = login,
                        Password = password,
                        IsaacSeed = isaacSeed,
                        CacheCRCs = cacheCrCs,
                        ClientId = Convert.ToHexString(userId),
                        DisplayMode = (DisplayMode)displayMode,
                        ClientSizeX = (int)screenSizeX,
                        ClientSizeY = (int)screenSizeY
                    };
                }
                return true;
                });
            message = decoded ? decodedMessage : default;
            return decoded;
        }
    }
}
