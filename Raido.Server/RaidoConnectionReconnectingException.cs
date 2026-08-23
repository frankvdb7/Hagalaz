using System;

namespace Raido.Server;

/// <summary>
/// Indicates that a send was attempted while a logical connection has no active physical transport.
/// </summary>
public sealed class RaidoConnectionReconnectingException : InvalidOperationException
{
    public RaidoConnectionReconnectingException(string connectionId)
        : base($"Raido connection '{connectionId}' is reconnecting and has no active physical transport.")
    {
    }
}
