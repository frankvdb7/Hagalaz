using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Raido.Common.Protocol;

namespace Raido.Server;

/// <summary>
/// Owns accepted physical connections and dispatches them to new or existing logical Raido connections.
/// </summary>
public sealed class RaidoConnectionDispatcher : ConnectionHandler
{
    private readonly RaidoConnectionSelector _selectConnection;
    private readonly IRaidoHubConnectionContextFactory _connectionFactory;
    private readonly RaidoHubConnectionHandler _connectionHandler;
    private readonly ILogger<RaidoConnectionDispatcher> _logger;

    public RaidoConnectionDispatcher(
        RaidoConnectionSelector selectConnection,
        IRaidoHubConnectionContextFactory connectionFactory,
        RaidoHubConnectionHandler connectionHandler,
        ILogger<RaidoConnectionDispatcher> logger)
    {
        _selectConnection = selectConnection ?? throw new ArgumentNullException(nameof(selectConnection));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionHandler = connectionHandler ?? throw new ArgumentNullException(nameof(connectionHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        RaidoConnectionSelection selection;
        try
        {
            selection = await _selectConnection(connection, connection.ConnectionClosed);
        }
        catch (Exception ex)
        {
            Log.SelectionFailed(_logger, ex);
            connection.Abort(new ConnectionAbortedException("The Raido connection selection failed.", ex));
            return;
        }

        if (selection.IsRejected)
        {
            connection.Abort(new ConnectionAbortedException("The Raido connection was rejected."));
            return;
        }

        if (selection.ExistingConnection is RaidoHubConnectionContext existingConnection)
        {
            if (!existingConnection.TryAttachPhysicalConnection(connection))
            {
                existingConnection.Abort();
                connection.Abort(new ConnectionAbortedException("The existing Raido connection rejected the physical connection."));
            }

            return;
        }

        if (selection.Protocol is not IRaidoProtocol protocol)
        {
            connection.Abort(new ConnectionAbortedException("The Raido connection selection was invalid."));
            return;
        }

        var logicalConnection = _connectionFactory.Create(
            connection,
            protocol,
            selection.StatefulReconnect);
        await _connectionHandler.ConnectAsync(logicalConnection);
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception> _selectionFailed = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, "ConnectionSelectionFailed"),
            "Raido connection selection failed.");

        public static void SelectionFailed(ILogger logger, Exception exception) => _selectionFailed(logger, exception);
    }
}

/// <summary>
/// Selects the logical Raido destination for an accepted physical connection.
/// </summary>
public delegate ValueTask<RaidoConnectionSelection> RaidoConnectionSelector(
    ConnectionContext connection,
    CancellationToken cancellationToken);
