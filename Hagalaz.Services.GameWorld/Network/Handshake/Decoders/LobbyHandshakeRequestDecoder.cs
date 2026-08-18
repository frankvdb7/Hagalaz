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

            // RSA header / block
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

                if (!xteaDataReader.TryRead(out byte gameId))
                {
                    return false;
                }

                if (!xteaDataReader.TryRead(out byte localeId))
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
