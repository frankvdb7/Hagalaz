using System;
using System.Buffers;
using Microsoft.Extensions.Logging;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoCodec<TProtocol> : IRaidoCodec<TProtocol> where TProtocol : IRaidoProtocol
    {
        private readonly IRaidoCodecFactory<TProtocol> _codecFactory;
        private readonly ILogger<DefaultRaidoCodec<TProtocol>> _logger;

        public DefaultRaidoCodec(IRaidoCodecFactory<TProtocol> codecFactory, ILogger<DefaultRaidoCodec<TProtocol>> logger)
        {
            _codecFactory = codecFactory;
            _logger = logger;
        }

        public bool TryDecodeMessage(int opcode, in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var messageDecoder = _codecFactory.GetMessageDecoder(opcode);
            if (messageDecoder == null)
            {
                Log.UnknownMessageOpcode(_logger, opcode);
                message = default;
                return false;
            }

            if (messageDecoder.TryDecodeMessage(input, out message))
            {
                return true;
            }
            Log.FailedDecodingMessage(_logger, opcode);
            message = default;
            return false;
        }

        public bool TryEncodeMessage<TMessage>(TMessage message, IRaidoMessageBinaryWriter output) where TMessage : RaidoMessage
        {
            var messageType = message.GetType();
            var messageEncoder = _codecFactory.GetMessageEncoder(messageType);
            if (messageEncoder == null)
            {
                Log.UnknownMessageEncoder(_logger, messageType.ToString());
                return false;
            }

            messageEncoder.EncodeMessage(message, output);
            return true;
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, string, Exception?> _unknownMessageEncoder = LoggerMessage.Define<string, string>(LogLevel.Debug,
                new EventId(1, "UnknownMessageEncoder"),
                "{Protocol}: No message encoder found for message '{MessageType}'");

            private static readonly Action<ILogger, string, int, Exception?> _unknownMessageOpcode = LoggerMessage.Define<string, int>(LogLevel.Debug,
                new EventId(2, "UnknownMessageDecoder"),
                "{Protocol}: Received unknown message with opcode '{MessageOpcode}'");

            private static readonly Action<ILogger, string, int, Exception?> _failedDecodingMessage = LoggerMessage.Define<string, int>(LogLevel.Warning,
                new EventId(3, "FailedDecodingMessage"),
                "{Protocol}: Failed decoding message with opcode '{MessageOpcode}'");

            public static void UnknownMessageEncoder(ILogger logger, string messageType) =>
                _unknownMessageEncoder(logger, typeof(TProtocol).Name, messageType, null);

            public static void UnknownMessageOpcode(ILogger logger, int opcode) => _unknownMessageOpcode(logger, typeof(TProtocol).Name, opcode, null);

            public static void FailedDecodingMessage(ILogger logger, int opcode) => _failedDecodingMessage(logger, typeof(TProtocol).Name, opcode, null);
        }
    }
}