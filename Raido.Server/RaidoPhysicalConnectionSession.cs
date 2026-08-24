using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Raido.Common.Protocol;

namespace Raido.Server;

/// <summary>
/// Owns one Kestrel connection for its complete physical lifetime.
/// </summary>
public sealed class RaidoPhysicalConnectionSession
{
    private readonly ConnectionContext _connection;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _switchLock = new(1, 1);
    private readonly TaskCompletionSource _failure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _terminated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _pumpsStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _sessionCts = new();
    private readonly object _stateLock = new();
    private RaidoApplicationConnection? _application;
    private RaidoConnectionContext? _logicalConnection;
    private CancellationTokenSource? _pumpCts;
    private Task? _inputPump;
    private Task? _outputPump;
    private bool _started;
    private bool _terminal;
    private RaidoApplicationTransfer? _reservedTransfer;

    public RaidoPhysicalConnectionSession(ConnectionContext connection, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _connection = connection;
        _logger = loggerFactory.CreateLogger<RaidoPhysicalConnectionSession>();
    }

    public string ConnectionId => _connection.ConnectionId;

    public CancellationToken ConnectionClosed => _connection.ConnectionClosed;

    public IFeatureCollection Features => _connection.Features;

    public System.Net.IPEndPoint? LocalEndPoint => _connection.LocalEndPoint as System.Net.IPEndPoint;

    public System.Net.IPEndPoint? RemoteEndPoint => _connection.RemoteEndPoint as System.Net.IPEndPoint;

    internal Task PumpsStopped => _pumpsStopped.Task;

    internal IDuplexPipe Transport => _connection.Transport;

    public async Task RunAsync(RaidoConnectionContext logicalConnection, RaidoApplicationConnection application)
    {
        lock (_stateLock)
        {
            if (_application is null)
            {
                _logicalConnection = logicalConnection;
                _application = application;
            }
        }
        StartPumps();

        try
        {
            var closedTask = Task.Delay(Timeout.InfiniteTimeSpan, _connection.ConnectionClosed);
            await Task.WhenAny(closedTask, _failure.Task).ConfigureAwait(false);
        }
        finally
        {
            await TerminateAsync().ConfigureAwait(false);
        }
    }

    internal void Attach(RaidoConnectionContext logicalConnection, RaidoApplicationConnection application)
    {
        ArgumentNullException.ThrowIfNull(logicalConnection);
        ArgumentNullException.ThrowIfNull(application);

        lock (_stateLock)
        {
            if (_terminal || _application is not null)
            {
                throw new InvalidOperationException("The physical session is already attached.");
            }

            _logicalConnection = logicalConnection;
            _application = application;
        }
    }

    internal bool CanAcceptTransfer(RaidoConnectionContext source)
    {
        lock (_stateLock)
        {
            return !_terminal && _started && ReferenceEquals(_logicalConnection, source) && _application is not null &&
                !_connection.ConnectionClosed.IsCancellationRequested;
        }
    }

    internal bool TryReserveTransfer(RaidoApplicationTransfer transfer)
    {
        lock (_stateLock)
        {
            if (_terminal || _reservedTransfer is not null || !ReferenceEquals(_logicalConnection, transfer.Source) ||
                _application is null || _connection.ConnectionClosed.IsCancellationRequested)
            {
                return false;
            }

            _reservedTransfer = transfer;
            return true;
        }
    }

    internal async Task<bool> CommitTransferAsync(
        RaidoConnectionContext source,
        RaidoConnectionContext target,
        RaidoApplicationConnection targetApplication,
        ReadOnlySequence<byte> pendingInput,
        IRaidoProtocol? protocol)
    {
        await _switchLock.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (_stateLock)
            {
                if (_terminal || !ReferenceEquals(_logicalConnection, source) || _application is null ||
                    _connection.ConnectionClosed.IsCancellationRequested)
                {
                    return false;
                }
            }

            await source.AcquireTransferWriteLockAsync().ConfigureAwait(false);
            try
            {
                await StopPumpsAsync().ConfigureAwait(false);
                await target.WaitForPreviousPhysicalPumpsAsync().ConfigureAwait(false);

                var oldApplication = _application!;
                await DrainOutputAsync(oldApplication.OutputReader).ConfigureAwait(false);

                if (!await target.CommitRebindAsync(this, protocol).ConfigureAwait(false))
                {
                    RaidoApplicationTransfer? failedTransfer;
                    lock (_stateLock)
                    {
                        failedTransfer = _reservedTransfer;
                        _reservedTransfer = null;
                    }
                    if (failedTransfer is not null)
                    {
                        target.RollbackRebind(failedTransfer);
                    }
                    StartPumps();
                    return false;
                }

                lock (_stateLock)
                {
                    _application = targetApplication;
                    _logicalConnection = target;
                }

                StartPumps();
                await target.InvokeReconnectedAsync().ConfigureAwait(false);
                await WritePendingInputAsync(targetApplication.InputWriter, pendingInput).ConfigureAwait(false);
                return true;
            }
            finally
            {
                source.ReleaseTransferWriteLock();
            }
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _reservedTransfer = null;
            }
            _failure.TrySetResult();
            source.AbortAllowReconnect(ex);
            return false;
        }
        finally
        {
            _switchLock.Release();
        }
    }

    internal void Abort()
    {
        _failure.TrySetResult();
        _connection.Transport.Input.CancelPendingRead();
        _connection.Transport.Output.CancelPendingFlush();
    }

    private void StartPumps()
    {
        lock (_stateLock)
        {
            if (_terminal || _pumpCts is not null || _application is null)
            {
                return;
            }

            _started = true;
            _pumpCts = CancellationTokenSource.CreateLinkedTokenSource(_sessionCts.Token);
            _inputPump = PumpInputAsync(_application, _pumpCts.Token);
            _outputPump = PumpOutputAsync(_application, _pumpCts.Token);
        }
    }

    private async Task StopPumpsAsync()
    {
        Task? input;
        Task? output;
        CancellationTokenSource? cts;
        lock (_stateLock)
        {
            cts = _pumpCts;
            input = _inputPump;
            output = _outputPump;
            _pumpCts = null;
            _inputPump = null;
            _outputPump = null;
        }

        if (cts is null)
        {
            _pumpsStopped.TrySetResult();
            return;
        }

        cts.Cancel();
        try
        {
            await Task.WhenAll(input!, output!).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();
            _pumpsStopped.TrySetResult();
        }
    }

    private async Task PumpInputAsync(RaidoApplicationConnection application, CancellationToken cancellationToken)
    {
        await application.AcquireInputOwnerAsync(cancellationToken).ConfigureAwait(false);
        ReadOnlySequence<byte> buffer = default;
        var readPending = false;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await _connection.Transport.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                buffer = result.Buffer;
                readPending = true;
                if (!buffer.IsEmpty)
                {
                    Copy(buffer, application.InputWriter);
                    await application.InputWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                _connection.Transport.Input.AdvanceTo(buffer.End);
                readPending = false;
                if (result.IsCompleted || (result.IsCanceled && !cancellationToken.IsCancellationRequested))
                {
                    _failure.TrySetResult();
                    return;
                }

                if (buffer.IsEmpty)
                {
                    await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "The physical input pump failed for connection {ConnectionId}.", ConnectionId);
            _failure.TrySetResult();
            GetLogicalConnection()?.AbortAllowReconnect(ex);
        }
        finally
        {
            if (readPending)
            {
                try { _connection.Transport.Input.AdvanceTo(buffer.End); } catch (InvalidOperationException) { }
            }
            application.ReleaseInputOwner();
        }
    }

    private async Task PumpOutputAsync(RaidoApplicationConnection application, CancellationToken cancellationToken)
    {
        await application.AcquireOutputOwnerAsync(cancellationToken).ConfigureAwait(false);
        ReadOnlySequence<byte> buffer = default;
        var readPending = false;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await application.OutputReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                buffer = result.Buffer;
                readPending = true;
                if (!buffer.IsEmpty)
                {
                    Copy(buffer, _connection.Transport.Output);
                    await _connection.Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                application.OutputReader.AdvanceTo(buffer.End);
                readPending = false;
                if (result.IsCompleted)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "The physical output pump failed for connection {ConnectionId}.", ConnectionId);
            _failure.TrySetResult();
            GetLogicalConnection()?.AbortAllowReconnect(ex);
        }
        finally
        {
            if (readPending)
            {
                try { application.OutputReader.AdvanceTo(buffer.End); } catch (InvalidOperationException) { }
            }
            application.ReleaseOutputOwner();
        }
    }

    private RaidoConnectionContext? GetLogicalConnection()
    {
        lock (_stateLock)
        {
            return _logicalConnection;
        }
    }

    private async Task DrainOutputAsync(PipeReader reader)
    {
        while (reader.TryRead(out var result))
        {
            var buffer = result.Buffer;
            if (!buffer.IsEmpty)
            {
                Copy(buffer, _connection.Transport.Output);
                await _connection.Transport.Output.FlushAsync().ConfigureAwait(false);
            }

            reader.AdvanceTo(buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }
    }

    private static async Task WritePendingInputAsync(PipeWriter writer, ReadOnlySequence<byte> pendingInput)
    {
        if (pendingInput.IsEmpty)
        {
            return;
        }

        Copy(pendingInput, writer);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private static void Copy(in ReadOnlySequence<byte> source, PipeWriter destination)
    {
        foreach (var segment in source)
        {
            var span = destination.GetSpan(segment.Length);
            segment.Span.CopyTo(span);
            destination.Advance(segment.Length);
        }
    }

    private async Task TerminateAsync()
    {
        bool notify;
        RaidoConnectionContext? logical;
        lock (_stateLock)
        {
            if (_terminal)
            {
                return;
            }

            _terminal = true;
            logical = _logicalConnection;
            notify = logical is not null;
        }

        _sessionCts.Cancel();
        await StopPumpsAsync().ConfigureAwait(false);

        try
        {
            await _connection.Transport.Input.CompleteAsync().ConfigureAwait(false);
            await _connection.Transport.Output.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "The physical transport could not be completed for connection {ConnectionId}.", ConnectionId);
        }

        if (notify)
        {
            logical!.OnPhysicalSessionEnded(this);
        }

        _terminated.TrySetResult();
    }
}
