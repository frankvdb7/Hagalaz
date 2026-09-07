using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Raido.Server.Extensions;

/// <summary>
/// Extension methods for configuring Raido on a Kestrel listener.
/// </summary>
public static class RaidoListenOptionsExtensions
{
    /// <summary>
    /// Configures the listener to dispatch accepted connections through Raido.
    /// </summary>
    public static IConnectionBuilder UseRaido(this ListenOptions listenOptions)
    {
        ArgumentNullException.ThrowIfNull(listenOptions);
        return listenOptions.UseConnectionHandler<RaidoConnectionDispatcher>();
    }
}
