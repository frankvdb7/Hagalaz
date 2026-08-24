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
internal sealed class RaidoPhysicalConnectionSession
{
    private readonly ConnectionContext _connection;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _switchLock = new(1, 1);
    private readonly TaskCompletionSource _failure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _sessionCts = new();
    private readonly object _stateLock = new();
    private RaidoApplicationConnection? _application;
    private RaidoConnectionContext? _logicalConnection;
    private PumpGeneration? _pumpGeneration;
    private Task _lastPumpsStopped = Task.CompletedTask;
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

    internal Task PumpsStopped
    {
        get
        {
            lock (_stateLock)
            {
                return _pumpGeneration?.Stopped.Task ?? _lastPumpsStopped;
            }
        }
    }

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

    internal RaidoApplicationTransfer? TakeReservedTransfer()
    {
        lock (_stateLock)
        {
            var transfer = _reservedTransfer;
            _reservedTransfer = null;
            return transfer;
        }
    }

    internal void ClearReservedTransfer(RaidoApplicationTransfer transfer)
    {
        lock (_stateLock)
        {
            if (ReferenceEquals(_reservedTransfer, transfer))
            {
                _reservedTransfer = null;
            }
        }
    }

    internal async Task<bool> CommitTransferAsync(
        RaidoApplicationTransfer transfer,
        RaidoConnectionContext source,
        RaidoConnectionContext target,
        RaidoApplicationConnection targetApplication,
        Func<ValueTask<ReadOnlyMemory<byte>>> capturePendingInput,
        IRaidoProtocol? protocol)
    {
        ArgumentNullException.ThrowIfNull(capturePendingInput);
        await _switchLock.WaitAsync().ConfigureAwait(false);
        var committed = false;
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
                // Stop this exact pump generation before the source protocol reader snapshots its suffix.
                await StopPumpsAsync().ConfigureAwait(false);
                await target.WaitForPreviousPhysicalPumpsAsync().ConfigureAwait(false);
                var pendingInput = await capturePendingInput().ConfigureAwait(false);

                var oldApplication = _application!;
                await DrainOutputAsync(oldApplication.OutputReader).ConfigureAwait(false);

                if (!target.CommitRebind(transfer))
                {
                    target.RollbackRebind();
                    StartPumps();
                    return false;
                }

                committed = true;
                lock (_stateLock)
                {
                    _application = targetApplication;
                    _logicalConnection = target;
                }

                // Install all bytes already observed by the replacement reader before the new
                // transport pump is allowed to deliver more input to the logical application.
                await WritePendingInputAsync(targetApplication.InputWriter, pendingInput).ConfigureAwait(false);
                StartPumps();
                await transfer.Reservation!.InvokeCommittedAsync().ConfigureAwait(false);
                await target.InvokeReconnectedAsync().ConfigureAwait(false);
                return true;
            }
            finally
            {
                source.ReleaseTransferWriteLock();
            }
        }

        catch (Exception ex)
        {
            ClearReservedTransfer();
            target.RollbackRebind();
            if (committed)
            {
                target.AbortAllowReconnect(ex);
            }
            else
            {
                source.AbortAllowReconnect(ex);
            }

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
            if (_terminal || _pumpGeneration is not null || _application is null)
            {
                return;
            }

            _started = true;
            var generation = new PumpGeneration(CancellationTokenSource.CreateLinkedTokenSource(_sessionCts.Token));
            generation.InputPump = PumpInputAsync(_application, generation.CancellationToken);
            generation.OutputPump = PumpOutputAsync(_application, generation.CancellationToken);
            _pumpGeneration = generation;
            _lastPumpsStopped = generation.Stopped.Task;
        }
    }

    private Task StopPumpsAsync()
    {
        _connection.Transport.Input.CancelPendingRead();
        _connection.Transport.Output.CancelPendingFlush();
        RaidoApplicationConnection? application;
        lock (_stateLock)
        {
            application = _application;
        }
        application?.InputWriter.CancelPendingFlush();
        application?.OutputReader.CancelPendingRead();

        PumpGeneration? generation;
        lock (_stateLock)
        {
            generation = _pumpGeneration;
            _pumpGeneration = null;
        }

        return generation?.StopAsync() ?? Task.CompletedTask;
    }

    private async Task PumpInputAsync(RaidoApplicationConnection application, CancellationToken cancellationToken)
    {
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
        }
    }

    private async Task PumpOutputAsync(RaidoApplicationConnection application, CancellationToken cancellationToken)
    {
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
        }
    }

    private RaidoConnectionContext? GetLogicalConnection()
    {
        lock (_stateLock)
        {
            return _logicalConnection;
        }
    }

    private void ClearReservedTransfer()
    {
        lock (_stateLock)
        {
            _reservedTransfer = null;
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

    private static async Task WritePendingInputAsync(PipeWriter writer, ReadOnlyMemory<byte> pendingInput)
    {
        if (pendingInput.IsEmpty)
        {
            return;
        }

        pendingInput.Span.CopyTo(writer.GetSpan(pendingInput.Length));
        writer.Advance(pendingInput.Length);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private static void Copy(in ReadOnlySequence<byte> source, PipeWriter destination)
    {
        foreach (var segment in source)
        {
            segment.Span.CopyTo(destination.GetSpan(segment.Length));
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
            _connection.Transport.Input.Complete();
            _connection.Transport.Output.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "The physical transport could not be completed for connection {ConnectionId}.", ConnectionId);
        }

        if (notify)
        {
            logical!.OnPhysicalSessionEnded(this);
        }

    }

    private sealed class PumpGeneration
    {
        private readonly object _lock = new();
        private readonly CancellationTokenSource _cts;
        private Task? _stopTask;

        public PumpGeneration(CancellationTokenSource cts) => _cts = cts;
        public CancellationToken CancellationToken => _cts.Token;
        public Task InputPump { get; set; } = Task.CompletedTask;
        public Task OutputPump { get; set; } = Task.CompletedTask;
        public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task StopAsync()
        {
            lock (_lock)
            {
                return _stopTask ??= StopCoreAsync();
            }
        }

        private async Task StopCoreAsync()
        {
            _cts.Cancel();
            try
            {
                await Task.WhenAll(InputPump, OutputPump).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _cts.Dispose();
                Stopped.TrySetResult();
            }
        }
    }
}
