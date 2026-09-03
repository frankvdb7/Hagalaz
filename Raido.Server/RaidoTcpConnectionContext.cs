using System;
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
    /// Represents the stable lower Raido connection and its physical transport.
    /// </summary>
    internal sealed class RaidoTcpConnectionContext : ConnectionContext, IConnectionHeartbeatFeature
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
        private readonly ILogger _logger;
        private readonly Lock _heartbeatLock = new();
        private readonly Lock _reconnectLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _statefulReconnectTimeout;

        private CancellationTokenRegistration? _closedRequestedRegistration;
        private ConnectionContext? _currentPhysicalConnection;
        private ConnectionContext? _detachedPhysicalConnection;
        private Task? _physicalInputTask;
        private Task? _applicationOutputTask;
        private TaskCompletionSource<bool>? _inputBoundaryWaiter;
        private TaskCompletionSource<bool>? _reconnectWaiter;
        private long? _reconnectWindowStartTimestamp;
        private Exception? _terminalException;

        private RaidoConnectionStatus _status;
        private bool _abortCallbackQueued;
        private bool _reconnectEnabled;
        private List<(Action<object> Callback, object State)>? _heartbeatHandlers;

        internal RaidoTcpConnectionContext(RaidoHubConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
            : this(contextOptions, loggerFactory, TimeProvider.System)
        {
        }

        internal RaidoTcpConnectionContext(
            RaidoHubConnectionContextOptions contextOptions,
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
            _features.Set<IConnectionHeartbeatFeature>(this);
        }

        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                _heartbeatHandlers ??= new List<(Action<object> Callback, object State)>();
                _heartbeatHandlers.Add((action, state));
            }
        }

        private void RunHeartbeat()
        {
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

        internal bool IsCurrentPhysicalConnection(ConnectionContext connection)
        {
            lock (_reconnectLock)
            {
                return ReferenceEquals(connection, _currentPhysicalConnection);
            }
        }

        internal bool TryActivatePersistentConnection(ConnectionContext replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);

            TaskCompletionSource<bool>? reconnectWaiter = null;
            ConnectionContext? detachedConnection = null;
            CancellationToken? detachedCloseRequestedToken = null;
            bool initialActivation;
            lock (_reconnectLock)
            {
                initialActivation = _status == RaidoConnectionStatus.Inactive && _connectionId is null;
                if (initialActivation)
                {
                    if (_status == RaidoConnectionStatus.Disposed)
                    {
                        return false;
                    }
                }
                else if (_status != RaidoConnectionStatus.Inactive || !_reconnectEnabled || _currentPhysicalConnection is not null ||
                         _reconnectWaiter is not TaskCompletionSource<bool> waiter || waiter.Task.IsCompleted ||
                         _detachedPhysicalConnection is not ConnectionContext currentDetachedConnection)
                {
                    return false;
                }
                else
                {
                    reconnectWaiter = waiter;
                    detachedConnection = currentDetachedConnection;
                    detachedCloseRequestedToken = detachedConnection.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested;
                }
            }

            CancellationTokenRegistration? closedRequestedRegistration = null;
            try
            {
                if (replacement.Features.Get<IConnectionLifetimeNotificationFeature>() is IConnectionLifetimeNotificationFeature lifetimeNotification)
                {
                    closedRequestedRegistration = lifetimeNotification.ConnectionClosedRequested.Register(
                        () => OnConnectionClosedRequested(replacement));
                }

                replacement.Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(
                    static state => ((RaidoTcpConnectionContext)state!).RunHeartbeat(), this);
            }
            catch
            {
                closedRequestedRegistration?.Dispose();
                throw;
            }

            var closedRequestedToken = replacement.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested;

            if (initialActivation)
            {
                var initialPublished = false;
                lock (_reconnectLock)
                {
                    if (_status == RaidoConnectionStatus.Inactive && _connectionId is null)
                    {
                        CopyStableFeatures(replacement.Features);

                        _connectionId = replacement.ConnectionId;
                        _currentPhysicalConnection = replacement;
                        _status = RaidoConnectionStatus.Active;
                        _closedRequestedRegistration = closedRequestedRegistration;
                        initialPublished = true;
                    }
                }

                if (!initialPublished)
                {
                    closedRequestedRegistration?.Dispose();
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
            CancellationTokenRegistration? obsoleteClosedRequestedRegistration = null;
            var queueAbortCallback = false;

            lock (_reconnectLock)
            {
                var reconnectWindowIsCurrent = _status == RaidoConnectionStatus.Inactive && _reconnectEnabled && _currentPhysicalConnection is null &&
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
                        out queueAbortCallback);
                }
                else if (reconnectWindowIsCurrent && !IsReconnectWindowExpiredLocked() &&
                    !replacement.ConnectionClosed.IsCancellationRequested &&
                    !closedRequestedToken.GetValueOrDefault().IsCancellationRequested)
                {
                    obsoleteClosedRequestedRegistration = _closedRequestedRegistration;
                    _currentPhysicalConnection = replacement;
                    _detachedPhysicalConnection = null;
                    _status = RaidoConnectionStatus.Active;
                    _closedRequestedRegistration = closedRequestedRegistration;
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
                        out queueAbortCallback);
                }
            }

            if (!published)
            {
                closedRequestedRegistration?.Dispose();
            }

            obsoleteClosedRequestedRegistration?.Dispose();

            if (terminal)
            {
                CompleteTerminalTransition(terminalConnection, terminalClosedRequestedRegistration, queueAbortCallback);
            }
            else if (published)
            {
                StartPhysicalInputPump();
            }

            return published;
        }

        private void StartApplicationOutputPump()
        {
            lock (_reconnectLock)
            {
                if (_applicationOutputTask is null)
                {
                    _applicationOutputTask = Task.Run(RunApplicationOutputAsync);
                }
            }
        }

        private void StartPhysicalInputPump()
        {
            lock (_reconnectLock)
            {
                if (_physicalInputTask is null)
                {
                    _physicalInputTask = Task.Run(RunPhysicalInputAsync);
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
                                    foreach (var segment in result.Buffer)
                                    {
                                        var destination = _application.Output.GetSpan(segment.Length);
                                        segment.Span.CopyTo(destination);
                                        _application.Output.Advance(segment.Length);
                                    }

                                    var flushResult = await _application.Output.FlushAsync(linkedCancellation.Token).ConfigureAwait(false);
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
            }
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
            }
            finally
            {
                output.Complete();
            }
        }

        internal RaidoConnectionStatus Status
        {
            get
            {
                lock (_reconnectLock)
                {
                    return _status;
                }
            }
        }

        internal bool IsTerminal => Status == RaidoConnectionStatus.Disposed;

        internal void AcknowledgeInputBoundary()
        {
            TaskCompletionSource<bool>? inputBoundaryWaiter;
            lock (_reconnectLock)
            {
                inputBoundaryWaiter = _inputBoundaryWaiter;
                _inputBoundaryWaiter = null;
            }

            inputBoundaryWaiter?.TrySetResult(true);
        }

        internal Exception? TerminalException
        {
            get
            {
                lock (_reconnectLock)
                {
                    return _terminalException;
                }
            }
        }

        internal bool IsReconnectEnabled
        {
            get
            {
                lock (_reconnectLock)
                {
                    return _reconnectEnabled && _status != RaidoConnectionStatus.Disposed;
                }
            }
        }

        internal bool TryGetCurrentConnection(out ConnectionContext connection)
        {
            lock (_reconnectLock)
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

        private async Task<bool> WaitForInputBoundaryAsync()
        {
            Task<bool>? inputBoundaryWaiter;
            lock (_reconnectLock)
            {
                if (_status == RaidoConnectionStatus.Disposed)
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
            lock (_reconnectLock)
            {
                if (_status == RaidoConnectionStatus.Disposed || !_reconnectEnabled)
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
            var queueAbortCallback = false;
            var timedOut = false;

            lock (_reconnectLock)
            {
                timedOut = TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: reconnectWaiter,
                    exception: null,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }

            if (timedOut)
            {
                CompleteTerminalTransition(currentConnection, closedRequestedRegistration, queueAbortCallback);
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
            var terminal = false;
            var queueAbortCallback = false;

            lock (_reconnectLock)
            {
                if (!ReferenceEquals(connection, _currentPhysicalConnection))
                {
                    reconnecting = false;
                    return false;
                }

                reconnecting = _status != RaidoConnectionStatus.Disposed && _reconnectEnabled;
                if (reconnecting)
                {
                    _currentPhysicalConnection = null;
                    _detachedPhysicalConnection = connection;
                    _status = RaidoConnectionStatus.Inactive;
                    _inputBoundaryWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
                        out queueAbortCallback);
                }
            }

            if (reconnecting)
            {
                // Set the current connection to null before waking the old transport so resulting completions are stale.
                connection.Transport.Input.CancelPendingRead();
                connection.Transport.Output.CancelPendingFlush();
                _transport.Input.CancelPendingRead();
            }
            else if (terminal)
            {
                CompleteTerminalTransition(terminalConnection, terminalClosedRequestedRegistration, queueAbortCallback);
            }

            return true;
        }

        private bool TryTransitionToTerminalLocked(
            ConnectionContext? expectedConnection,
            TaskCompletionSource<bool>? expectedWaiter,
            Exception? exception,
            out ConnectionContext? currentConnection,
            out CancellationTokenRegistration? closedRequestedRegistration,
            out bool queueAbortCallback)
        {
            currentConnection = null;
            closedRequestedRegistration = null;
            queueAbortCallback = false;

            if (_status == RaidoConnectionStatus.Disposed ||
                (expectedConnection is not null && !ReferenceEquals(expectedConnection, _currentPhysicalConnection)) ||
                (expectedWaiter is not null && !ReferenceEquals(expectedWaiter, _reconnectWaiter)))
            {
                return false;
            }

            _status = RaidoConnectionStatus.Disposed;
            _reconnectEnabled = false;
            _terminalException = exception;
            currentConnection = _currentPhysicalConnection;
            _currentPhysicalConnection = null;
            _detachedPhysicalConnection = null;
            closedRequestedRegistration = _closedRequestedRegistration;
            _closedRequestedRegistration = null;

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
            bool queueAbortCallback)
        {
            closedRequestedRegistration?.Dispose();
            AcknowledgeInputBoundary();

            if (currentConnection is not null)
            {
                // Physical cancellation wakes transport operations without changing the stable connection identity.
                currentConnection.Transport.Output.CancelPendingFlush();
                currentConnection.Transport.Input.CancelPendingRead();
                currentConnection.Abort();
            }

            CompleteStablePipes();

            if (queueAbortCallback)
            {
                // We fire and forget since this can trigger user code to run.
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        private void CompleteStablePipes()
        {
            _transport.Input.CancelPendingRead();
            _transport.Output.CancelPendingFlush();
            _application.Input.CancelPendingRead();
            _application.Output.CancelPendingFlush();

            // The handler owns the stable transport reader and completes it when its reader is disposed.
            // Complete only the producer ends here so an in-flight reader can observe terminal completion
            // and still advance its current read before disposing the reader.
            _transport.Output.Complete();
            _application.Output.Complete();
        }

        private ConnectionContext? GetCurrentPhysicalConnection()
        {
            lock (_reconnectLock)
            {
                return _currentPhysicalConnection;
            }
        }

        private void CopyStableFeatures(IFeatureCollection physicalFeatures)
        {
            foreach (var (featureType, feature) in physicalFeatures)
            {
                if (featureType == typeof(IConnectionHeartbeatFeature) ||
                    featureType == typeof(IConnectionLifetimeNotificationFeature) ||
                    featureType == typeof(IConnectionInherentKeepAliveFeature))
                {
                    continue;
                }

                _features[featureType] = feature;
            }
        }

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
            bool queueAbortCallback;
            bool terminal;

            lock (_reconnectLock)
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
                    out queueAbortCallback);
            }

            if (terminal)
            {
                CompleteTerminalTransition(currentConnection, closedRequestedRegistration, queueAbortCallback);
            }
        }

        internal bool TryAbortForConnection(ConnectionContext connection, Exception exception)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration? closedRequestedRegistration;
            bool queueAbortCallback;
            bool terminal;

            lock (_reconnectLock)
            {
                terminal = TryTransitionToTerminalLocked(
                    expectedConnection: connection,
                    expectedWaiter: null,
                    exception,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }
            if (terminal)
            {
                CompleteTerminalTransition(currentConnection, closedRequestedRegistration, queueAbortCallback);
            }
            return terminal;
        }

        internal bool TryAbortForCurrentConnection(Exception exception)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration? closedRequestedRegistration;
            bool queueAbortCallback;
            bool terminal;

            lock (_reconnectLock)
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
                    out queueAbortCallback);
            }

            if (terminal)
            {
                CompleteTerminalTransition(currentConnection, closedRequestedRegistration, queueAbortCallback);
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
            var queueAbortCallback = false;

            lock (_reconnectLock)
            {
                TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: null,
                    exception,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }

            CompleteTerminalTransition(currentConnection, closedRequestedRegistration, queueAbortCallback);
        }

        internal void Cleanup()
        {
            CancellationTokenRegistration? closedRequestedRegistration;
            TaskCompletionSource<bool>? reconnectWaiter;
            lock (_reconnectLock)
            {
                closedRequestedRegistration = _closedRequestedRegistration;
                _closedRequestedRegistration = null;
                _currentPhysicalConnection = null;
                _detachedPhysicalConnection = null;
                _status = RaidoConnectionStatus.Disposed;
                _reconnectEnabled = false;
                _reconnectWindowStartTimestamp = null;
                reconnectWaiter = _reconnectWaiter;
                _reconnectWaiter = null;
            }

            closedRequestedRegistration?.Dispose();
            _transportExecutionCancellation.Cancel();
            AcknowledgeInputBoundary();
            CompleteStablePipes();
            reconnectWaiter?.TrySetResult(false);
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
        public override CancellationToken ConnectionClosed => _connectionAbortedTokenSource.Token;
        public override IPEndPoint? LocalEndPoint => GetCurrentPhysicalConnection()?.LocalEndPoint as IPEndPoint;
        public override IPEndPoint? RemoteEndPoint => GetCurrentPhysicalConnection()?.RemoteEndPoint as IPEndPoint;

        public override void Abort()
        {
            ConnectionContext? currentConnection = null;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            var queueAbortCallback = false;

            lock (_reconnectLock)
            {
                TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: null,
                    exception: null,
                    out currentConnection,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }

            CompleteTerminalTransition(currentConnection, closedRequestedRegistration, queueAbortCallback);
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

            public static void AbortFailed(ILogger logger, Exception exception) => _abortFailed(logger, exception);
        }

        private sealed class LocalDuplexPipe(PipeReader input, PipeWriter output) : IDuplexPipe
        {
            public PipeReader Input { get; } = input;
            public PipeWriter Output { get; } = output;
        }

    }

    internal enum RaidoConnectionStatus
    {
        Inactive,
        Active,
        Disposed
    }
}
