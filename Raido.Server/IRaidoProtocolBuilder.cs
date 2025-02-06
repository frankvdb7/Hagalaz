using Microsoft.Extensions.DependencyInjection;
using Raido.Common.Protocol;

namespace Raido.Server
{

    public interface IRaidoProtocolBuilder<TProtocol> where TProtocol : class, IRaidoProtocol
    {
        IServiceCollection Services { get; }
        IRaidoProtocolBuilder<TProtocol> AddDecoder<TDecoder>(int opcode) where TDecoder : class, IRaidoMessageDecoder;
        IRaidoProtocolBuilder<TProtocol> AddEncoder<TEncoder>() where TEncoder : class, IRaidoMessageEncoder;
    }
}