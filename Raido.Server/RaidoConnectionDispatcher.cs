using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raido.Common.Protocol;

namespace Raido.Server;

/// <summary>
/// Owns the accepted physical connection and exposes only logical dispatch operations to the application.
/// </summary>
public sealed class RaidoConnectionDispatchContext
{
    private readonly ConnectionContext _connection;
    private readonly IRaidoHubConnectionContextFactory _connectionFactory;
    private readonly RaidoHubConnectionHandler _connectionHandler;
    private int _dispatchState;

    internal RaidoConnectionDispatchContext(
        ConnectionContext connection,
        IRaidoHubConnectionContextFactory connectionFactory,
        RaidoHubConnectionHandler connectionHandler)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionHandler = connectionHandler ?? throw new ArgumentNullException(nameof(connectionHandler));
    }

    internal bool WasDispatched => Volatile.Read(ref _dispatchState) == 2;

    internal bool WasAborted => Volatile.Read(ref _dispatchState) == 3;

    public async ValueTask DispatchNewAsync(
        IRaidoProtocol protocol,
        bool statefulReconnect,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        BeginDispatch();
        cancellationToken.ThrowIfCancellationRequested();

        var logicalConnection = _connectionFactory.Create(
            _connection,
            protocol,
            statefulReconnect);
        Volatile.Write(ref _dispatchState, 2);
        await _connectionHandler.ConnectAsync(logicalConnection).ConfigureAwait(false);
    }

    public async ValueTask<bool> DispatchExistingAsync(
        RaidoHubConnectionContext connection,
        Func<CancellationToken, ValueTask> prepareAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(prepareAsync);
        BeginDispatch();

        cancellationToken.ThrowIfCancellationRequested();
        if (!connection.IsAwaitingReconnect)
        {
            return false;
        }

        try
        {
            await prepareAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Volatile.Write(ref _dispatchState, 3);
            _connection.Abort(exception as ConnectionAbortedException ?? new ConnectionAbortedException(
                "The Raido reconnect could not be completed safely.",
                exception));
            throw;
        }

        try
        {
            if (!connection.TryAttachPhysicalConnection(_connection))
            {
                throw new ConnectionAbortedException(
                    "The logical Raido connection rejected the prepared physical connection.");
            }

            Volatile.Write(ref _dispatchState, 2);
            return true;
        }
        catch (Exception exception)
        {
            Volatile.Write(ref _dispatchState, 3);
            connection.Abort();
            _connection.Abort(exception as ConnectionAbortedException ?? new ConnectionAbortedException(
                "The Raido reconnect could not be completed safely.",
                exception));
            throw;
        }
    }

    private void BeginDispatch()
    {
        if (Interlocked.CompareExchange(ref _dispatchState, 1, 0) != 0)
        {
            throw new InvalidOperationException("A physical Raido connection can only be dispatched once.");
        }
    }
}

/// <summary>
/// Handles one accepted physical connection using a scoped application delegate.
/// </summary>
public sealed class RaidoConnectionDispatcher : ConnectionHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRaidoHubConnectionContextFactory _connectionFactory;
    private readonly RaidoHubConnectionHandler _connectionHandler;
    private readonly ILogger<RaidoConnectionDispatcher> _logger;

    public RaidoConnectionDispatcher(
        IServiceScopeFactory scopeFactory,
        IRaidoHubConnectionContextFactory connectionFactory,
        RaidoHubConnectionHandler connectionHandler,
        ILogger<RaidoConnectionDispatcher> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionHandler = connectionHandler ?? throw new ArgumentNullException(nameof(connectionHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var application = scope.ServiceProvider.GetRequiredService<RaidoConnectionDelegate>();
        var dispatch = new RaidoConnectionDispatchContext(
            connection,
            _connectionFactory,
            _connectionHandler);

        try
        {
            await application(connection, dispatch, connection.ConnectionClosed).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.ApplicationFailed(_logger, exception);
            if (!dispatch.WasAborted)
            {
                connection.Abort(exception as ConnectionAbortedException ?? new ConnectionAbortedException(
                    "The Raido application connection failed.",
                    exception));
            }
            return;
        }

        if (!dispatch.WasDispatched)
        {
            connection.Abort(new ConnectionAbortedException(
                "The Raido application connection did not dispatch the physical connection."));
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception> _applicationFailed = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, "ApplicationConnectionFailed"),
            "Raido application connection failed.");

        public static void ApplicationFailed(ILogger logger, Exception exception) => _applicationFailed(logger, exception);
    }
}

/// <summary>
/// Handles one accepted physical connection at the application boundary.
/// </summary>
public delegate Task RaidoConnectionDelegate(
    ConnectionContext connection,
    RaidoConnectionDispatchContext dispatch,
    CancellationToken cancellationToken);
