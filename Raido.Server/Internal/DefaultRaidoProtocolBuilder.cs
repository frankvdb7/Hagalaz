using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoProtocolBuilder<TProtocol> : IRaidoProtocolBuilder<TProtocol> where TProtocol : class, IRaidoProtocol
    {
        private readonly RaidoCodecStore<TProtocol> _store = new();

        public IServiceCollection Services { get; }

        internal DefaultRaidoProtocolBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Services.TryAddSingleton(provider => _store);
        }

        public IRaidoProtocolBuilder<TProtocol> AddDecoder<TDecoder>(int opcode) where TDecoder : class, IRaidoMessageDecoder
        {
            _store.AddDecoder<TDecoder>(opcode);
            Services.TryAddTransient<TDecoder>();
            return this;
        }

        public IRaidoProtocolBuilder<TProtocol> AddEncoder<TEncoder>() where TEncoder : class, IRaidoMessageEncoder
        {
            _store.AddEncoder<TEncoder>();
            Services.TryAddTransient<TEncoder>();
            return this;
        }
    }
}