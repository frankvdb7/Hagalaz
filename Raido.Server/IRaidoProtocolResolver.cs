using System.Collections.Generic;
using Raido.Common.Protocol;

namespace Raido.Server;

/// <summary>
/// A resolver abstraction for working with <see cref="IRaidoProtocol"/> instances.
/// </summary>
public interface IRaidoProtocolResolver
{
    /// <summary>
    /// Gets a collection of all available protocols.
    /// </summary>
    IReadOnlyList<IRaidoProtocol> AllProtocols { get; }

    /// <summary>
    /// Gets the protocol with the specified name, if it is allowed by the specified list of supported protocols.
    /// </summary>
    /// <param name="protocolName">The protocol name.</param>
    /// <param name="supportedProtocols">A collection of supported protocols.</param>
    /// <returns>A matching <see cref="IRaidoProtocol"/> or <c>null</c> if no matching protocol was found.</returns>
    IRaidoProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols);
}