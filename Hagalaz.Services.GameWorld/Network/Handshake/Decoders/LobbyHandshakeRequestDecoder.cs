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
    public class LobbyHandshakeRequestDecoder : IRaidoMessageDecoder
    {
        private readonly IOptions<RsaClientConfig> _rsaOptions;
        private readonly ICacheAPI _cacheApi;

        public LobbyHandshakeRequestDecoder(IOptions<RsaClientConfig> rsaOptions, ICacheAPI cacheApi)
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

            var rsaKeys = _rsaOptions.Value;
            RaidoMessage? decodedMessage = default;
            var decoded = HandshakeDecoderHelper.TryParseSignInPayload(ref reader, rsaKeys.PrivateKey, rsaKeys.ModulusKey,
                (ref SequenceReader<byte> xteaDataReader, uint[] isaacSeed, string password, string login) =>
                {

                if (!xteaDataReader.TryRead(out byte gameId))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte localeId))
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

                if (!xteaDataReader.TryRead(out string staticClientId))
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out int affiliateId))
                {
                    return false;
                }

                if (!xteaDataReader.TryReadBigEndian(out int staticClientNumber))
                {
                    return false;
                }

                // TODO - validate servertoken
                if (!xteaDataReader.TryRead(out string serverToken))
                {
                    return false;
                }

                // start cache CRC block
                if (!HandshakeDecoderHelper.TryReadCacheCrcs(ref xteaDataReader, _cacheApi, out var cacheCrCs))
                {
                    return false;
                }

                // stop cache CRC block
                decodedMessage = new LobbySignInRequest()
                {
                    ClientRevision = clientRevision,
                    ClientRevisionPatch = clientRevisionPatch,
                    Login = login,
                    Password = password,
                    IsaacSeed = isaacSeed,
                    CacheCRCs = cacheCrCs,
                    ClientId = Convert.ToHexString(userId),
                    DisplayMode = DisplayMode.LobbyScreen
                };
                return true;
                });
            message = decoded ? decodedMessage : default;
            return decoded;
        }
    }
}
