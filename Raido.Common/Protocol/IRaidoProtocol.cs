using System;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// A protocol abstraction for communicating with TCP.
    /// </summary>
    public interface IRaidoProtocol : IRaidoMessageWriter<RaidoMessage>, IRaidoMessageReader<RaidoMessage>
    {
        /// <summary>
        /// Gets the name of the protocol. The name is used to resolve the protocol between the client and server.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the major version of the protocol.
        /// </summary>
        int Version { get; }
        
        /// <summary>
        /// Gets a value indicating whether the protocol supports the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>A value indicating whether the protocol supports the specified version.</returns>
        bool IsVersionSupported(int version);

        /// <summary>
        /// Gets the message in bytes.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message);
    }
}