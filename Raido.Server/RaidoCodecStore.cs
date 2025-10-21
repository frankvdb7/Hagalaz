using System;
using System.Collections.Generic;
using System.Linq;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A store for message codecs for a specific protocol.
    /// </summary>
    /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
    public class RaidoCodecStore<TProtocol> where TProtocol : IRaidoProtocol
    {
        private readonly Dictionary<int, Type> _decoders = new();
        private readonly Dictionary<Type, Type> _encoders = new();

        /// <summary>
        /// Adds a message decoder to the store.
        /// </summary>
        /// <typeparam name="TDecoder">The type of the decoder.</typeparam>
        /// <param name="opcode">The opcode of the message.</param>
        /// <exception cref="ArgumentException">Thrown when a decoder for the specified opcode already exists.</exception>
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

        /// <summary>
        /// Adds a message encoder to the store.
        /// </summary>
        /// <typeparam name="TEncoder">The type of the encoder.</typeparam>
        /// <exception cref="TypeAccessException">Thrown when the encoder type does not implement <see cref="IRaidoMessageEncoder{TMessage}"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when an encoder for the message type already exists.</exception>
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

        /// <summary>
        /// Tries to get a message decoder for the specified opcode.
        /// </summary>
        /// <param name="opcode">The opcode of the message.</param>
        /// <param name="decoderType">When this method returns, contains the decoder type, if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if a decoder for the specified opcode is found; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Tries to get a message encoder for the specified message type.
        /// </summary>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="encoderType">When this method returns, contains the encoder type, if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if an encoder for the specified message type is found; otherwise, <c>false</c>.</returns>
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