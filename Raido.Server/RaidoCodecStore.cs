using System;
using System.Collections.Generic;
using System.Linq;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public class RaidoCodecStore<TProtocol> where TProtocol : IRaidoProtocol
    {
        private readonly Dictionary<int, Type> _decoders = new();
        private readonly Dictionary<Type, Type> _encoders = new();

        public void AddDecoder<TDecoder>(int opcode) where TDecoder : IRaidoMessageDecoder
        {
            var type = typeof(TDecoder);
            try
            {
                _decoders.Add(opcode, type);
            }
            catch (ArgumentException argumentException)
            {
                throw new ArgumentException($"Decoder for {nameof(opcode)} '{opcode}' already exists", argumentException);
            }
        }

        public void AddEncoder<TEncoder>() where TEncoder : IRaidoMessageEncoder
        {
            var encoderType = typeof(TEncoder);
            var raidoMessageEncoder = encoderType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRaidoMessageEncoder<>));
            if (raidoMessageEncoder == null)
            {
                throw new TypeAccessException();
            }

            var messageType = raidoMessageEncoder.GenericTypeArguments[0];
            try
            {
                _encoders.Add(messageType, encoderType);
            }
            catch (ArgumentException argumentException)
            {
                throw new ArgumentException($"Encoder for message '{messageType.Name}' already exists", argumentException);
            }
        }

        public bool TryGetDecoder(int opcode, out Type decoderType)
        {
            if (_decoders.TryGetValue(opcode, out var decoder))
            {
                decoderType = decoder;
                return true;
            }

            decoderType = default!;
            return false;
        }

        public bool TryGetEncoder(Type messageType, out Type encoderType)
        {
            if (_encoders.TryGetValue(messageType, out var encoder))
            {
                encoderType = encoder;
                return true;
            }

            encoderType = default!;
            return false;
        }
    }
}