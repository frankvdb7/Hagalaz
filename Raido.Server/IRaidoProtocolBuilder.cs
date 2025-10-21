using Microsoft.Extensions.DependencyInjection;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A builder for configuring a Raido protocol.
    /// </summary>
    /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
    public interface IRaidoProtocolBuilder<TProtocol> where TProtocol : class, IRaidoProtocol
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where the protocol services are configured.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Adds a message decoder to the protocol.
        /// </summary>
        /// <typeparam name="TDecoder">The type of the decoder.</typeparam>
        /// <param name="opcode">The opcode of the message.</param>
        /// <returns>The protocol builder.</returns>
        IRaidoProtocolBuilder<TProtocol> AddDecoder<TDecoder>(int opcode) where TDecoder : class, IRaidoMessageDecoder;

        /// <summary>
        /// Adds a message encoder to the protocol.
        /// </summary>
        /// <typeparam name="TEncoder">The type of the encoder.</typeparam>
        /// <returns>The protocol builder.</returns>
        IRaidoProtocolBuilder<TProtocol> AddEncoder<TEncoder>() where TEncoder : class, IRaidoMessageEncoder;
    }
}