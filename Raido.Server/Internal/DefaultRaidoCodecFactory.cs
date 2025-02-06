using System;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoCodecFactory<TProtocol> : IRaidoCodecFactory<TProtocol> where TProtocol : IRaidoProtocol
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RaidoCodecStore<TProtocol> _codecStore;

        public DefaultRaidoCodecFactory(IServiceProvider serviceProvider, RaidoCodecStore<TProtocol> codecStore)
        {
            _serviceProvider = serviceProvider;
            _codecStore = codecStore;
        }

        public IRaidoMessageDecoder? GetMessageDecoder(int opcode)
        {
            if (_codecStore.TryGetDecoder(opcode, out var decoderType))
            {
                return _serviceProvider.GetService(decoderType) as IRaidoMessageDecoder;
            }

            return null;
        }

        public IRaidoMessageEncoder? GetMessageEncoder(Type messageType)
        {
            if (_codecStore.TryGetEncoder(messageType, out var encoderType))
            {
                return _serviceProvider.GetService(encoderType) as IRaidoMessageEncoder;
            }

            return null;
        }
    }
}