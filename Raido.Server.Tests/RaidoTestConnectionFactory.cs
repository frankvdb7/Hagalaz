using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Raido.Common.Protocol;
using Raido.Server;

namespace Raido.Server.Tests;

internal static class RaidoTestConnectionFactory
{
    public static RaidoHubConnectionContext Create(
        ConnectionContext physicalConnection,
        RaidoConnectionContextOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        TimeProvider? timeProvider = null,
        IRaidoProtocol? protocol = null)
    {
        options ??= new RaidoConnectionContextOptions();
        loggerFactory ??= NullLoggerFactory.Instance;
        timeProvider ??= TimeProvider.System;

        var tcpConnection = new RaidoTcpConnectionContext(options, loggerFactory, timeProvider);
        if (!tcpConnection.TryAttachPhysicalConnection(physicalConnection))
        {
            throw new InvalidOperationException("The initial physical connection could not be activated.");
        }

        return new RaidoHubConnectionContext(
            tcpConnection,
            options,
            protocol ?? new TestProtocol { ParseMessageReturns = false },
            loggerFactory,
            timeProvider);
    }
}
