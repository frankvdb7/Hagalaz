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
            RaidoMessage? decodedMessage = default;
            var decoded = HandshakeDecoderHelper.TryParseSignInPayload(ref reader, rsaKeys.PrivateKey, rsaKeys.ModulusKey,
                (ref SequenceReader<byte> xteaDataReader, uint[] isaacSeed, string password, string login) =>
                {

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

                if (!HandshakeDecoderHelper.TryReadUserId(ref xteaDataReader, out var userId))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out string settings))
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out int affliateId))
                {
                    return false;
                }

                if (!HandshakeDecoderHelper.TrySkipSettingsData(ref xteaDataReader))
                {
                    return false;
                }

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
                if (!HandshakeDecoderHelper.TryReadCacheCrcs(ref xteaDataReader, _cacheApi, out var cacheCrCs))
                {
                    return false;
                }

                // stop cache CRC block

                decodedMessage = isReconnect
                    ? new WorldReconnectRequest
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
                        ClientSizeY = screenSizeY,
                        LobbySessionClaimId = loggedInFromLobby && !string.IsNullOrWhiteSpace(serverToken)
                            ? serverToken
                            : null
                    }
                    : new WorldSignInRequest
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
                        ClientSizeY = screenSizeY,
                        LobbySessionClaimId = loggedInFromLobby && !string.IsNullOrWhiteSpace(serverToken)
                            ? serverToken
                            : null
                    };
                return true;
                });
            message = decoded ? decodedMessage : default;
            return decoded;
        }
    }
}
