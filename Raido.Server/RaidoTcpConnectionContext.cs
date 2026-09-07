using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Raido.Server
{
    /// <summary>
    /// Represents one stable Raido TCP connection across physical transport attachments.
    /// </summary>
    internal sealed class RaidoTcpConnectionContext : ConnectionContext,
        IConnectionHeartbeatFeature,
        IConnectionIdFeature,
        IConnectionItemsFeature,
        IConnectionTransportFeature,
        IConnectionLifetimeFeature,
        IConnectionLifetimeNotificationFeature,
        IConnectionInherentKeepAliveFeature
    {
        private static readonly WaitCallback _abortedCallback = AbortConnection;
        private static readonly TimeSpan MaxSupportedReconnectTimeout =
            TimeSpan.FromMilliseconds(uint.MaxValue - 1L);

        private readonly TaskCompletionSource _abortCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IDuplexPipe _transport;
        private readonly IDuplexPipe _application;
        private readonly CancellationTokenSource _transportExecutionCancellation = new();
        private readonly IFeatureCollection _features = new FeatureCollection();
        private readonly IDictionary<object, object?> _items = new ConcurrentDictionary<object, object?>();
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new();
        private readonly CancellationTokenSource _connectionClosedRequestedTokenSource = new();
        private readonly ILogger _logger;
        private readonly Lock _heartbeatLock = new();
        private readonly Lock _stateLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _statefulReconnectTimeout;

        private CancellationTokenRegistration? _closedRequestedRegistration;
        private CancellationTokenRegistration? _connectionClosedRegistration;
        private ConnectionContext? _currentPhysicalConnection;
        private ConnectionContext? _detachedPhysicalConnection;
        private Task? _physicalInputTask;
        private Task? _applicationOutputTask;
        private bool _stablePipesSignaled;
        private bool _applicationOutputCompleted;
        private bool _transportInputCompleted;
        private bool _transportOutputCompleted;
        private TaskCompletionSource<bool>? _inputBoundaryWaiter;
        private TaskCompletionSource<bool>? _outputBoundaryWaiter;
        private TaskCompletionSource<bool>? _reconnectWaiter;
        private long? _reconnectWindowStartTimestamp;
        private Exception? _detachedTransportException;
        private Exception? _terminalException;

        private bool _hasActivated;
        private bool _hasInherentKeepAlive;
        private bool _disposed;
        private bool _abortCallbackQueued;
        private bool _reconnectEnabled;
        private List<(Action<object> Callback, object State)>? _heartbeatHandlers;

        internal RaidoTcpConnectionContext(RaidoConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
            : this(contextOptions, loggerFactory, TimeProvider.System)
        {
        }

        internal RaidoTcpConnectionContext(
            RaidoConnectionContextOptions contextOptions,
            ILoggerFactory loggerFactory,
            TimeProvider timeProvider)
        {
            if (contextOptions.StatefulReconnectEnabled &&
                (contextOptions.StatefulReconnectTimeout <= TimeSpan.Zero ||
                 contextOptions.StatefulReconnectTimeout > MaxSupportedReconnectTimeout))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contextOptions.StatefulReconnectTimeout),
                    contextOptions.StatefulReconnectTimeout,
                    "Stateful reconnect timeout must be greater than zero and within the supported .NET timer range.");
            }

            _logger = loggerFactory.CreateLogger<RaidoTcpConnectionContext>();
            _statefulReconnectTimeout = contextOptions.StatefulReconnectTimeout;
            _reconnectEnabled = contextOptions.StatefulReconnectEnabled;
            _timeProvider = timeProvider;
            var input = new Pipe();
            var output = new Pipe();
            _transport = new LocalDuplexPipe(input.Reader, output.Writer);
            _application = new LocalDuplexPipe(output.Reader, input.Writer);
            _features.Set<IConnectionIdFeature>(this);
            _features.Set<IConnectionItemsFeature>(this);
            _features.Set<IConnectionTransportFeature>(this);
            _features.Set<IConnectionLifetimeFeature>(this);
            _features.Set<IConnectionLifetimeNotificationFeature>(this);
            _features.Set<IConnectionHeartbeatFeature>(this);
            _features.Set<IConnectionInherentKeepAliveFeature>(this);
        }

        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                _heartbeatHandlers ??= new List<(Action<object> Callback, object State)>();
                _heartbeatHandlers.Add((action, state));
            }
        }

        private void RunHeartbeat(ConnectionContext physicalConnection)
        {
            lock (_stateLock)
            {
                if (!ReferenceEquals(physicalConnection, _currentPhysicalConnection) ||
                    _inputBoundaryWaiter is not null)
                {
                    return;
                }

                lock (_heartbeatLock)
                {
                    if (_heartbeatHandlers is null)
                    {
                        return;
                    }

                    foreach (var (callback, state) in _heartbeatHandlers)
                    {
                        callback(state);
                    }
                }
            }
        }

        private bool IsCurrentPhysicalConnection(ConnectionContext connection)
        {
            lock (_stateLock)
            {
                return ReferenceEquals(connection, _currentPhysicalConnection);
            }
        }

        internal bool TryAttachPhysicalConnection(ConnectionContext replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);

            TaskCompletionSource<bool>? reconnectWaiter = null;
            Task<bool>? outputBoundaryTask = null;
            ConnectionContext? detachedConnection = null;
            CancellationToken? detachedCloseRequestedToken = null;
            bool initialActivation;
            lock (_stateLock)
            {
                initialActivation = !_hasActivated && !_disposed;
                if (!initialActivation)
                {
                    if (_disposed || !_reconnectEnabled || _currentPhysicalConnection is not null ||
                        _reconnectWaiter is not TaskCompletionSource<bool> waiter || waiter.Task.IsCompleted ||
                        _detachedPhysicalConnection is not ConnectionContext currentDetachedConnection)
                    {
                        return false;
                    }

                    reconnectWaiter = waiter;
                    outputBoundaryTask = _outputBoundaryWaiter?.Task;
                    detachedConnection = currentDetachedConnection;
                    detachedCloseRequestedToken = detachedConnection.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested;
                }
            }

            CancellationTokenRegistration? closedRequestedRegistration = null;
            CancellationTokenRegistration? connectionClosedRegistration = null;
            try
            {
                connectionClosedRegistration = replacement.ConnectionClosed.Register(
                    () => OnPhysicalConnectionClosed(replacement));

                if (replacement.Features.Get<IConnectionLifetimeNotificationFeature>() is IConnectionLifetimeNotificationFeature lifetimeNotification)
                {
                    closedRequestedRegistration = lifetimeNotification.ConnectionClosedRequested.Register(
                        () => OnConnectionClosedRequested(replacement));
                }

                replacement.Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(
                    static state =>
                    {
                        var heartbeatState = ((RaidoTcpConnectionContext Context, ConnectionContext PhysicalConnection))state!;
                        heartbeatState.Context.RunHeartbeat(heartbeatState.PhysicalConnection);
                    },
                    (this, replacement));
                var closedRequestedToken = replacement.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested;

                if (!initialActivation && outputBoundaryTask is not null && !outputBoundaryTask.GetAwaiter().GetResult())
                {
                    return false;
                }

                if (initialActivation)
                {
                    var initialPublished = false;
                    lock (_stateLock)
                    {
                        if (!_disposed && !_hasActivated)
                        {
                            CopyLogicalFeatures(replacement.Features);
                            CopyInitialItems(replacement);

                            _connectionId = replacement.ConnectionId;
                            _currentPhysicalConnection = replacement;
                            Volatile.Write(ref _hasInherentKeepAlive, HasInherentKeepAlive(replacement));
                            _hasActivated = true;
                            _connectionClosedRegistration = connectionClosedRegistration;
                            connectionClosedRegistration = null;
                            _closedRequestedRegistration = closedRequestedRegistration;
                            closedRequestedRegistration = null;
                            initialPublished = true;
                        }
                    }

                    if (!initialPublished)
                    {
                        return false;
                    }

                    StartApplicationOutputPump();
                    StartPhysicalInputPump();

                    // A physical connection may already be closed when it is activated. Publish ownership first,
                    // then apply that state through the normal physical-disconnect path.
                    if (replacement.ConnectionClosed.IsCancellationRequested)
                    {
                        OnPhysicalConnectionClosed(replacement);
                    }

                    if (closedRequestedToken.GetValueOrDefault().IsCancellationRequested)
                    {
                        OnConnectionClosedRequested(replacement);
                    }

                    return true;
                }

                var activationWaiter = reconnectWaiter!;
                var detachedPhysicalConnection = detachedConnection!;
                var published = false;
                var terminal = false;
                ConnectionContext? terminalConnection = null;
                CancellationTokenRegistration? terminalClosedRequestedRegistration = null;
                CancellationTokenRegistration? terminalConnectionClosedRegistration = null;
                CancellationTokenRegistration? obsoleteClosedRequestedRegistration = null;
                CancellationTokenRegistration? obsoleteConnectionClosedRegistration = null;
                var queueAbortCallback = false;

                lock (_stateLock)
                {
                    var reconnectWindowIsCurrent = !_disposed && _reconnectEnabled && _currentPhysicalConnection is null &&
                        ReferenceEquals(detachedPhysicalConnection, _detachedPhysicalConnection) &&
                        ReferenceEquals(activationWaiter, _reconnectWaiter) && !activationWaiter.Task.IsCompleted;

                    if (reconnectWindowIsCurrent && detachedCloseRequestedToken.GetValueOrDefault().IsCancellationRequested)
                    {
                        terminal = TryTransitionToTerminalLocked(
                            expectedConnection: null,
                            expectedWaiter: activationWaiter,
                            exception: null,
                            out terminalConnection,
                            out terminalClosedRequestedRegistration,
                            out terminalConnectionClosedRegistration,
                            out queueAbortCallback);
                    }
                    else if (reconnectWindowIsCurrent && !IsReconnectWindowExpiredLocked() &&
                        !replacement.ConnectionClosed.IsCancellationRequested &&
                        !closedRequestedToken.GetValueOrDefault().IsCancellationRequested)
                    {
                        obsoleteClosedRequestedRegistration = _closedRequestedRegistration;
                        obsoleteConnectionClosedRegistration = _connectionClosedRegistration;
                        _currentPhysicalConnection = replacement;
                        Volatile.Write(ref _hasInherentKeepAlive, HasInherentKeepAlive(replacement));
                        _detachedPhysicalConnection = null;
                        _detachedTransportException = null;
                        _connectionClosedRegistration = connectionClosedRegistration;
                        connectionClosedRegistration = null;
                        _closedRequestedRegistration = closedRequestedRegistration;
                        closedRequestedRegistration = null;
                        _reconnectWaiter = null;
                        _reconnectWindowStartTimestamp = null;
                        activationWaiter.TrySetResult(true);
                        published = true;
                    }
                    else if (reconnectWindowIsCurrent && IsReconnectWindowExpiredLocked())
                    {
                        terminal = TryTransitionToTerminalLocked(
                            expectedConnection: null,
                            expectedWaiter: activationWaiter,
                            exception: null,
                            out terminalConnection,
                            out terminalClosedRequestedRegistration,
                            out terminalConnectionClosedRegistration,
                            out queueAbortCallback);
                    }
                }

                obsoleteClosedRequestedRegistration?.Dispose();
                obsoleteConnectionClosedRegistration?.Dispose();

                if (terminal)
                {
                    CompleteTerminalTransition(
                        terminalConnection,
                        terminalClosedRequestedRegistration,
                        terminalConnectionClosedRegistration,
                        queueAbortCallback);
                }
                else if (published)
                {
                    StartPhysicalInputPump();
                }

                return published;
            }
            finally
            {
                closedRequestedRegistration?.Dispose();
                connectionClosedRegistration?.Dispose();
            }
        }

        private void StartApplicationOutputPump()
        {
            lock (_stateLock)
            {
                if (_applicationOutputTask is null)
                {
                    // The first read may complete synchronously when buffered data is available;
                    // keep relay work, including physical flushing, off the lifecycle lock.
                    _applicationOutputTask = ObserveRelayTask(Task.Run(RunApplicationOutputAsync));
                }
            }
        }

        private void StartPhysicalInputPump()
        {
            lock (_stateLock)
            {
                if (_physicalInputTask is null)
                {
                    // The first read may complete synchronously when buffered data is available;
                    // keep relay work, including physical flushing, off the lifecycle lock.
                    _physicalInputTask = ObserveRelayTask(Task.Run(RunPhysicalInputAsync));
                }
            }
        }

        private async Task RunPhysicalInputAsync()
        {
            try
            {
                while (!_transportExecutionCancellation.IsCancellationRequested)
                {
                    if (!TryGetCurrentConnection(out var physicalConnection))
                    {
                        if (!await WaitForReconnectAsync().ConfigureAwait(false))
                        {
                            break;
                        }

                        continue;
                    }

                    if (!await WaitForInputBoundaryAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        _transportExecutionCancellation.Token,
                        physicalConnection.ConnectionClosed);
                    var input = physicalConnection.Transport.Input;
                    try
                    {
                        while (true)
                        {
                            ReadResult result;
                            try
                            {
                                result = await input.ReadAsync(linkedCancellation.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
                            {
                                break;
                            }

                            try
                            {
                                if (!result.Buffer.IsEmpty)
                                {
                                    var buffer = result.Buffer;
                                    if (!TryCommitPhysicalInput(physicalConnection, in buffer, out var flushTask))
                                    {
                                        break;
                                    }

                                    // Physical cancellation stops subsequent reads, but cannot cancel the
                                    // commit of bytes already copied into the stable input pipe.
                                    var flushResult = await flushTask.ConfigureAwait(false);
                                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                                    {
                                        break;
                                    }
                                }
                            }
                            finally
                            {
                                input.AdvanceTo(result.Buffer.End);
                            }

                            if (result.IsCanceled || result.IsCompleted)
                            {
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
                    {
                    }
                    catch (Exception ex) when (!_transportExecutionCancellation.IsCancellationRequested)
                    {
                        HandleTransportFailure(physicalConnection, ex);
                    }

                    if (_transportExecutionCancellation.IsCancellationRequested)
                    {
                        break;
                    }

                    if (IsCurrentPhysicalConnection(physicalConnection))
                    {
                        OnPhysicalConnectionClosed(physicalConnection);
                    }
                }
            }
            catch (OperationCanceledException) when (_transportExecutionCancellation.IsCancellationRequested)
            {
                // Terminal cleanup canceled the relay.
                return;
            }
            finally
            {
                CompleteApplicationOutput();
            }
        }

        private bool TryCommitPhysicalInput(
            ConnectionContext physicalConnection,
            in ReadOnlySequence<byte> buffer,
            out ValueTask<FlushResult> flushTask)
        {
            lock (_stateLock)
            {
                if (_disposed || !ReferenceEquals(physicalConnection, _currentPhysicalConnection))
                {
                    flushTask = default;
                    return false;
                }

                foreach (var segment in buffer)
                {
                    var destination = _application.Output.GetSpan(segment.Length);
                    segment.Span.CopyTo(destination);
                    _application.Output.Advance(segment.Length);
                }

                flushTask = _application.Output.FlushAsync(CancellationToken.None);
                return true;
            }
        }

        private Task ObserveRelayTask(Task task)
        {
            task.ContinueWith(
                static (completedTask, state) =>
                {
                    var context = (RaidoTcpConnectionContext)state!;
                    context.AbortWithException(completedTask.Exception!.GetBaseException());
                },
                this,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return task;
        }

        private async Task RunApplicationOutputAsync()
        {
            var output = _application.Input;
            try
            {
                while (true)
                {
                    ReadResult result;
                    try
                    {
                        result = await output.ReadAsync(_transportExecutionCancellation.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (_transportExecutionCancellation.IsCancellationRequested)
                    {
                        break;
                    }

                    if (result.IsCanceled)
                    {
                        output.AdvanceTo(result.Buffer.End);
                        AcknowledgeOutputBoundary();
                        if (result.IsCompleted)
                        {
                            break;
                        }

                        continue;
                    }

                    ConnectionContext? physicalConnection = null;
                    try
                    {
                        if (!result.Buffer.IsEmpty && (physicalConnection = GetCurrentPhysicalConnection()) is not null)
                        {
                            var physicalOutput = physicalConnection.Transport.Output;
                            foreach (var segment in result.Buffer)
                            {
                                var destination = physicalOutput.GetSpan(segment.Length);
                                segment.Span.CopyTo(destination);
                                physicalOutput.Advance(segment.Length);
                            }

                            var flushResult = await physicalOutput.FlushAsync(_transportExecutionCancellation.Token).ConfigureAwait(false);
                            if ((flushResult.IsCanceled || flushResult.IsCompleted) &&
                                !_transportExecutionCancellation.IsCancellationRequested)
                            {
                                HandleTransportFailure(
                                    physicalConnection,
                                    new IOException("The physical transport output was closed."));
                            }
                        }
                    }
                    catch (OperationCanceledException) when (_transportExecutionCancellation.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex) when (!_transportExecutionCancellation.IsCancellationRequested)
                    {
                        if (physicalConnection is not null)
                        {
                            HandleTransportFailure(physicalConnection, ex);
                        }
                    }
                    finally
                    {
                        output.AdvanceTo(result.Buffer.End);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (_transportExecutionCancellation.IsCancellationRequested)
            {
                return;
            }
            finally
            {
                output.Complete();
            }
        }

        internal bool IsActive
        {
            get
            {
                lock (_stateLock)
                {
                    return !_disposed && _currentPhysicalConnection is not null;
                }
            }
        }

        internal bool IsTerminal
        {
            get
            {
                lock (_stateLock)
                {
                    return _disposed;
                }
            }
        }

        internal void AcknowledgeInputBoundary()
        {
            TaskCompletionSource<bool>? inputBoundaryWaiter;
            lock (_stateLock)
            {
                inputBoundaryWaiter = _inputBoundaryWaiter;
                _inputBoundaryWaiter = null;
            }

            inputBoundaryWaiter?.TrySetResult(true);
        }

        internal void CompleteTransportInput()
        {
            lock (_stateLock)
            {
                if (_transportInputCompleted)
                {
                    return;
                }

                _transportInputCompleted = true;
            }

            _transport.Input.Complete();
        }

        internal void CompleteTransportOutput()
        {
            lock (_stateLock)
            {
                if (_transportOutputCompleted)
                {
                    return;
                }

                _transportOutputCompleted = true;
            }

            _transport.Output.Complete();
        }

        private void AcknowledgeOutputBoundary(bool result = true)
        {
            TaskCompletionSource<bool>? outputBoundaryWaiter;
            lock (_stateLock)
            {
                outputBoundaryWaiter = _outputBoundaryWaiter;
                _outputBoundaryWaiter = null;
            }

            outputBoundaryWaiter?.TrySetResult(result);
        }

        internal Exception? TerminalException
        {
            get
            {
                lock (_stateLock)
                {
                    return _terminalException;
                }
            }
        }

        internal bool IsReconnectEnabled
        {
            get
            {
                lock (_stateLock)
                {
                    return _reconnectEnabled && !_disposed;
                }
            }
        }

        internal bool IsAwaitingReconnect
        {
            get
            {
                lock (_stateLock)
                {
                    return !_disposed && _reconnectEnabled && _currentPhysicalConnection is null &&
                        _reconnectWaiter is TaskCompletionSource<bool> reconnectWaiter &&
                        !reconnectWaiter.Task.IsCompleted && _detachedPhysicalConnection is not null &&
                        !IsReconnectWindowExpiredLocked();
                }
            }
        }

        internal bool TryGetCurrentConnection(out ConnectionContext connection)
        {
            lock (_stateLock)
            {
                if (_currentPhysicalConnection is null)
                {
                    connection = null!;
                    return false;
                }

                connection = _currentPhysicalConnection;
                return true;
            }
        }

        internal Task<bool> WaitForReconnectAsync() => WaitForReconnectAsync(_statefulReconnectTimeout);

        internal async Task<bool> WaitForInputBoundaryAsync()
        {
            Task<bool>? inputBoundaryWaiter;
            lock (_stateLock)
            {
                if (_disposed)
                {
                    return false;
                }

                inputBoundaryWaiter = _inputBoundaryWaiter?.Task;
            }

            return inputBoundaryWaiter is null || await inputBoundaryWaiter.ConfigureAwait(false);
        }

        internal async Task<bool> WaitForReconnectAsync(TimeSpan timeout)
        {
            TaskCompletionSource<bool>? reconnectWaiter;
            TimeSpan remainingTimeout;
            lock (_stateLock)
            {
                if (_disposed || !_reconnectEnabled)
                {
                    return false;
                }

                if (_currentPhysicalConnection is not null)
                {
                    return true;
                }

                reconnectWaiter = _reconnectWaiter;

                if (reconnectWaiter is null)
                {
                    return false;
                }

                remainingTimeout = GetReconnectWaitTimeoutLocked(timeout);
            }

            if (remainingTimeout <= TimeSpan.Zero)
            {
                return TimeoutReconnect(reconnectWaiter);
            }

            try
            {
                return await reconnectWaiter.Task.WaitAsync(remainingTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return TimeoutReconnect(reconnectWaiter);
            }
        }



        private bool TimeoutReconnect(TaskCompletionSource<bool> reconnectWaiter)
        {
            ConnectionContext? currentConnection = null;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            CancellationTokenRegistration? connectionClosedRegistration = null;
            var queueAbortCallback = false;
            var timedOut = false;

            lock (_stateLock)
            {
                timedOut = TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: reconnectWaiter,
                    exception: null,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out connectionClosedRegistration,
                    out queueAbortCallback);
            }

            if (timedOut)
            {
                CompleteTerminalTransition(
                    currentConnection,
                    closedRequestedRegistration,
                    connectionClosedRegistration,
                    queueAbortCallback);
                return false;
            }

            return reconnectWaiter.Task.IsCompletedSuccessfully && reconnectWaiter.Task.Result;
        }

        internal void OnPhysicalConnectionClosed(ConnectionContext connection) => TryDetachPhysicalConnection(connection, exception: null, out _);

        internal bool HandleTransportFailure(ConnectionContext connection, Exception exception)
        {
            var isCurrent = IsCurrentPhysicalConnection(connection);
            var handled = TryDetachPhysicalConnection(connection, exception, out var reconnecting);
            return !isCurrent || reconnecting || !handled;
        }

        private bool TryDetachPhysicalConnection(ConnectionContext connection, Exception? exception, out bool reconnecting)
        {
            ConnectionContext? terminalConnection = null;
            CancellationTokenRegistration? terminalClosedRequestedRegistration = null;
            CancellationTokenRegistration? terminalConnectionClosedRegistration = null;
            var terminal = false;
            var queueAbortCallback = false;

            lock (_stateLock)
            {
                if (!ReferenceEquals(connection, _currentPhysicalConnection))
                {
                    reconnecting = false;
                    return false;
                }

                reconnecting = !_disposed && _reconnectEnabled;
                if (reconnecting)
                {
                    _currentPhysicalConnection = null;
                    _detachedPhysicalConnection = connection;
                    _detachedTransportException = exception;
                    _inputBoundaryWaiter ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _outputBoundaryWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _reconnectWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _reconnectWindowStartTimestamp = _timeProvider.GetTimestamp();
                }
                else
                {
                    terminal = TryTransitionToTerminalLocked(
                        expectedConnection: connection,
                        expectedWaiter: null,
                        exception,
                        out terminalConnection,
                        out terminalClosedRequestedRegistration,
                        out terminalConnectionClosedRegistration,
                        out queueAbortCallback);
                }
            }

            if (reconnecting)
            {
                // Set the current connection to null before waking the old transport so resulting completions are stale.
                connection.Transport.Input.CancelPendingRead();
                connection.Transport.Output.CancelPendingFlush();
                _application.Input.CancelPendingRead();
                _transport.Input.CancelPendingRead();
            }
            else if (terminal)
            {
                CompleteTerminalTransition(
                    terminalConnection,
                    terminalClosedRequestedRegistration,
                    terminalConnectionClosedRegistration,
                    queueAbortCallback);
            }

            return true;
        }

        private bool TryTransitionToTerminalLocked(
            ConnectionContext? expectedConnection,
            TaskCompletionSource<bool>? expectedWaiter,
            Exception? exception,
            out ConnectionContext? currentConnection,
            out CancellationTokenRegistration? closedRequestedRegistration,
            out CancellationTokenRegistration? connectionClosedRegistration,
            out bool queueAbortCallback)
        {
            currentConnection = null;
            closedRequestedRegistration = null;
            connectionClosedRegistration = null;
            queueAbortCallback = false;

            if (_disposed ||
                (expectedConnection is not null && !ReferenceEquals(expectedConnection, _currentPhysicalConnection)) ||
                (expectedWaiter is not null && !ReferenceEquals(expectedWaiter, _reconnectWaiter)))
            {
                return false;
            }

            _disposed = true;
            _reconnectEnabled = false;
            _terminalException = exception ?? _detachedTransportException;
            _detachedTransportException = null;
            currentConnection = _currentPhysicalConnection;
            _currentPhysicalConnection = null;
            _detachedPhysicalConnection = null;
            closedRequestedRegistration = _closedRequestedRegistration;
            _closedRequestedRegistration = null;
            connectionClosedRegistration = _connectionClosedRegistration;
            _connectionClosedRegistration = null;

            var reconnectWaiter = _reconnectWaiter;
            _reconnectWaiter = null;
            _reconnectWindowStartTimestamp = null;
            reconnectWaiter?.TrySetResult(false);

            if (!_abortCallbackQueued)
            {
                _abortCallbackQueued = true;
                queueAbortCallback = true;
            }

            return true;
        }

        private void CompleteTerminalTransition(
            ConnectionContext? currentConnection,
            CancellationTokenRegistration? closedRequestedRegistration,
            CancellationTokenRegistration? connectionClosedRegistration,
            bool queueAbortCallback)
        {
            closedRequestedRegistration?.Dispose();
            connectionClosedRegistration?.Dispose();
            AcknowledgeInputBoundary();
            AcknowledgeOutputBoundary(false);

            if (currentConnection is not null)
            {
                // Physical cancellation wakes transport operations without changing the stable connection identity.
                currentConnection.Transport.Output.CancelPendingFlush();
                currentConnection.Transport.Input.CancelPendingRead();
                currentConnection.Abort();
            }

            SignalStablePipes();

            if (queueAbortCallback)
            {
                // We fire and forget since this can trigger user code to run.
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        private void SignalStablePipes()
        {
            lock (_stateLock)
            {
                if (_stablePipesSignaled)
                {
                    return;
                }

                _stablePipesSignaled = true;
            }

            _transport.Input.CancelPendingRead();
            _transport.Output.CancelPendingFlush();
            _application.Input.CancelPendingRead();
            _application.Output.CancelPendingFlush();

            // Producer-owned pipe ends are completed only after their execution contexts have quiesced.
            // These cancellations only wake pending operations so terminal signalling never races a
            // producer's GetSpan/Advance/FlushAsync sequence.
        }

        private void CompleteApplicationOutput()
        {
            lock (_stateLock)
            {
                if (_applicationOutputCompleted)
                {
                    return;
                }

                _applicationOutputCompleted = true;
            }

            _application.Output.Complete();
        }

        private ConnectionContext? GetCurrentPhysicalConnection()
        {
            lock (_stateLock)
            {
                return _currentPhysicalConnection;
            }
        }

        private void CopyLogicalFeatures(IFeatureCollection physicalFeatures)
        {
            if (physicalFeatures.Get<IConnectionUserFeature>() is IConnectionUserFeature userFeature)
            {
                _features.Set(userFeature);
            }
        }

        private void CopyInitialItems(ConnectionContext physicalConnection)
        {
            foreach (var (key, value) in physicalConnection.Items)
            {
                _items[key] = value;
            }
        }

        private static bool HasInherentKeepAlive(ConnectionContext physicalConnection) =>
            physicalConnection.Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive == true;

        private TimeSpan GetReconnectWaitTimeoutLocked(TimeSpan requestedTimeout)
        {
            var remainingTimeout = _statefulReconnectTimeout;
            if (_reconnectWindowStartTimestamp is long startTimestamp)
            {
                remainingTimeout -= _timeProvider.GetElapsedTime(startTimestamp);
            }

            if (requestedTimeout != Timeout.InfiniteTimeSpan && requestedTimeout < remainingTimeout)
            {
                remainingTimeout = requestedTimeout;
            }

            return remainingTimeout;
        }

        private bool IsReconnectWindowExpiredLocked() =>
            _reconnectWindowStartTimestamp is long startTimestamp &&
            _timeProvider.GetElapsedTime(startTimestamp) >= _statefulReconnectTimeout;

        private void OnConnectionClosedRequested(ConnectionContext connection)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration? closedRequestedRegistration;
            CancellationTokenRegistration? connectionClosedRegistration;
            bool queueAbortCallback;
            bool terminal;

            lock (_stateLock)
            {
                var isCurrentConnection = ReferenceEquals(connection, _currentPhysicalConnection);
                var isDetachedConnection = ReferenceEquals(connection, _detachedPhysicalConnection) &&
                    _reconnectEnabled && _reconnectWaiter is not null && !_reconnectWaiter.Task.IsCompleted;
                if (!isCurrentConnection && !isDetachedConnection)
                {
                    return;
                }

                terminal = TryTransitionToTerminalLocked(
                    expectedConnection: isCurrentConnection ? connection : null,
                    expectedWaiter: null,
                    exception: null,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out connectionClosedRegistration,
                    out queueAbortCallback);
            }

            if (terminal)
            {
                SignalConnectionClosedRequested();
                CompleteTerminalTransition(
                    currentConnection,
                    closedRequestedRegistration,
                    connectionClosedRegistration,
                    queueAbortCallback);
            }
        }

        internal bool TryAbortForConnection(ConnectionContext connection, Exception exception)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration? closedRequestedRegistration;
            CancellationTokenRegistration? connectionClosedRegistration;
            bool queueAbortCallback;
            bool terminal;

            lock (_stateLock)
            {
                terminal = TryTransitionToTerminalLocked(
                    expectedConnection: connection,
                    expectedWaiter: null,
                    exception,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out connectionClosedRegistration,
                    out queueAbortCallback);
            }
            if (terminal)
            {
                CompleteTerminalTransition(
                    currentConnection,
                    closedRequestedRegistration,
                    connectionClosedRegistration,
                    queueAbortCallback);
            }
            return terminal;
        }

        internal bool TryAbortIfActive(Exception exception)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration? closedRequestedRegistration;
            CancellationTokenRegistration? connectionClosedRegistration;
            bool queueAbortCallback;
            bool terminal;

            lock (_stateLock)
            {
                if (_currentPhysicalConnection is null)
                {
                    return false;
                }

                terminal = TryTransitionToTerminalLocked(
                    expectedConnection: _currentPhysicalConnection,
                    expectedWaiter: null,
                    exception,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out connectionClosedRegistration,
                    out queueAbortCallback);
            }

            if (terminal)
            {
                CompleteTerminalTransition(
                    currentConnection,
                    closedRequestedRegistration,
                    connectionClosedRegistration,
                    queueAbortCallback);
            }

            return terminal;
        }

        internal Task AbortAsync()
        {
            Abort();
            return _abortCompletedTcs.Task;
        }

        internal void AbortWithException(Exception exception)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration? closedRequestedRegistration;
            CancellationTokenRegistration? connectionClosedRegistration;
            var queueAbortCallback = false;

            lock (_stateLock)
            {
                TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: null,
                    exception,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out connectionClosedRegistration,
                    out queueAbortCallback);
            }

            CompleteTerminalTransition(
                currentConnection,
                closedRequestedRegistration,
                connectionClosedRegistration,
                queueAbortCallback);
        }

        internal async Task CleanupAsync()
        {
            // Let callers release any producer-owned operation that currently holds the state lock before
            // the terminal transition acquires it.
            await Task.Yield();
            Abort();
            await _abortCompletedTcs.Task.ConfigureAwait(false);

            CancellationTokenRegistration? closedRequestedRegistration;
            CancellationTokenRegistration? connectionClosedRegistration;
            TaskCompletionSource<bool>? reconnectWaiter;
            ConnectionContext? currentConnection;
            ConnectionContext? detachedConnection;
            Task? physicalInputTask;
            Task? applicationOutputTask;
            lock (_stateLock)
            {
                closedRequestedRegistration = _closedRequestedRegistration;
                _closedRequestedRegistration = null;
                connectionClosedRegistration = _connectionClosedRegistration;
                _connectionClosedRegistration = null;
                currentConnection = _currentPhysicalConnection;
                detachedConnection = _detachedPhysicalConnection;
                _currentPhysicalConnection = null;
                _detachedPhysicalConnection = null;
                _disposed = true;
                _reconnectEnabled = false;
                _detachedTransportException = null;
                _reconnectWindowStartTimestamp = null;
                reconnectWaiter = _reconnectWaiter;
                _reconnectWaiter = null;
                physicalInputTask = _physicalInputTask;
                applicationOutputTask = _applicationOutputTask;
            }

            closedRequestedRegistration?.Dispose();
            connectionClosedRegistration?.Dispose();
            _transportExecutionCancellation.Cancel();
            AcknowledgeInputBoundary();
            AcknowledgeOutputBoundary(false);
            SignalStablePipes();
            reconnectWaiter?.TrySetResult(false);

            CancelPhysicalTransport(currentConnection);
            if (detachedConnection is not null && !ReferenceEquals(detachedConnection, currentConnection))
            {
                CancelPhysicalTransport(detachedConnection);
            }

            await AwaitRelayTaskAsync(physicalInputTask).ConfigureAwait(false);
            await AwaitRelayTaskAsync(applicationOutputTask).ConfigureAwait(false);
            CompleteApplicationOutput();
        }

        private static void CancelPhysicalTransport(ConnectionContext? connection)
        {
            if (connection is null)
            {
                return;
            }

            connection.Transport.Input.CancelPendingRead();
            connection.Transport.Output.CancelPendingFlush();
        }

        private async Task AwaitRelayTaskAsync(Task? relayTask)
        {
            if (relayTask is null)
            {
                return;
            }

            try
            {
                await relayTask.ConfigureAwait(false);
            }
            catch (Exception) when (IsTerminal)
            {
                // Unexpected relay failures have already terminalized the logical connection.
            }
        }

        private string _connectionId = null!;

        public override string ConnectionId
        {
            get => _connectionId;
            set => _connectionId = value;
        }
        public override IFeatureCollection Features => _features;
        public override IDictionary<object, object?> Items
        {
            get => _items;
            set => throw new NotSupportedException("The stable connection owns its Items collection.");
        }
        public override IDuplexPipe Transport
        {
            get => _transport;
            set => throw new NotSupportedException("The stable connection owns its physical transport.");
        }

        internal IDuplexPipe Application => _application;

        internal bool TryWriteStableTransport(
            Action<PipeWriter> write,
            CancellationToken cancellationToken,
            out bool hasWrittenBytes,
            out ValueTask<FlushResult> flushTask)
        {
            lock (_stateLock)
            {
                if (_disposed || _currentPhysicalConnection is null)
                {
                    hasWrittenBytes = false;
                    flushTask = default;
                    return false;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    hasWrittenBytes = false;
                    flushTask = ValueTask.FromCanceled<FlushResult>(cancellationToken);
                    return true;
                }

                // Serialization is synchronous. Keeping it under the lifecycle lock preserves the
                // no-replay linearization. The stable flush is invoked before releasing the lock so
                // an admitted message is committed before a physical detach can publish a boundary.
                write(_transport.Output);
                hasWrittenBytes = !_transport.Output.CanGetUnflushedBytes || _transport.Output.UnflushedBytes > 0;
                flushTask = _transport.Output.FlushAsync(CancellationToken.None);
                return true;
            }
        }

        public override CancellationToken ConnectionClosed => _connectionAbortedTokenSource.Token;
        public CancellationToken ConnectionClosedRequested
        {
            get => _connectionClosedRequestedTokenSource.Token;
            set => throw new NotSupportedException("The stable connection owns its close-request token.");
        }
        public override IPEndPoint? LocalEndPoint => GetCurrentPhysicalConnection()?.LocalEndPoint as IPEndPoint;
        public override IPEndPoint? RemoteEndPoint => GetCurrentPhysicalConnection()?.RemoteEndPoint as IPEndPoint;
        bool IConnectionInherentKeepAliveFeature.HasInherentKeepAlive => Volatile.Read(ref _hasInherentKeepAlive);

        public void RequestClose()
        {
            SignalConnectionClosedRequested();
            Abort();
        }

        private void SignalConnectionClosedRequested()
        {
            try
            {
                _ = ObserveCloseRequestedCallbacksAsync(_connectionClosedRequestedTokenSource.CancelAsync());
            }
            catch (Exception ex)
            {
                Log.CloseRequestedFailed(_logger, ex);
            }
        }

        private async Task ObserveCloseRequestedCallbacksAsync(Task cancellationTask)
        {
            try
            {
                await cancellationTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.CloseRequestedFailed(_logger, ex);
            }
        }

        public override void Abort()
        {
            ConnectionContext? currentConnection = null;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            CancellationTokenRegistration? connectionClosedRegistration = null;
            var queueAbortCallback = false;

            lock (_stateLock)
            {
                TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: null,
                    exception: null,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out connectionClosedRegistration,
                    out queueAbortCallback);
            }

            CompleteTerminalTransition(
                currentConnection,
                closedRequestedRegistration,
                connectionClosedRegistration,
                queueAbortCallback);
        }

        private static void AbortConnection(object? state)
        {
            var connection = (RaidoTcpConnectionContext)state!;

            try
            {
                connection._connectionAbortedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Log.AbortFailed(connection._logger, ex);
            }
            finally
            {
                connection._abortCompletedTcs.TrySetResult();
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(4, "AbortFailed"), "Abort callback failed.");
            private static readonly Action<ILogger, Exception> _closeRequestedFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(5, "CloseRequestedFailed"), "Close-request notification failed.");

            public static void AbortFailed(ILogger logger, Exception exception) => _abortFailed(logger, exception);
            public static void CloseRequestedFailed(ILogger logger, Exception exception) => _closeRequestedFailed(logger, exception);
        }

        private sealed class LocalDuplexPipe(PipeReader input, PipeWriter output) : IDuplexPipe
        {
            public PipeReader Input { get; } = input;
            public PipeWriter Output { get; } = output;
        }

    }
}
