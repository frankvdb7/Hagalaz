using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Tests
{
    public class TestMessage : RaidoMessage
    {
    }

    public class TestProtocol : IRaidoProtocol
    {
        public string Name => "test";
        public int Version => 1;
        public bool ParseMessageReturns { get; set; } = true;
        public RaidoMessage? MessageToReturn { get; set; }

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, [MaybeNullWhen(false)] out RaidoMessage message)
        {
            if (!ParseMessageReturns)
            {
                message = null;
                return false;
            }

            consumed = input.End;
            examined = input.End;
            message = MessageToReturn;
            return true;
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
        {
            // Not needed for these tests
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        public bool IsVersionSupported(int version)
        {
            return true;
        }
    }

    [TestClass]
    public class RaidoConnectionHandlerTests
    {
        private ILoggerFactory _loggerFactory = null!;
        private IOptions<RaidoOptions> _raidoOptions = null!;
        private IRaidoLifetimeManager _lifetimeManager = null!;
        private IRaidoDispatcher _dispatcher = null!;
        private RaidoMetrics _metrics = null!;
        private IMeterFactory _meterFactory = null!;
        private RaidoConnectionHandler _connectionHandler = null!;
        private RaidoConnectionContext _connection = null!;
        private DefaultConnectionContext _connectionContext = null!;
        private PipeReader _pipeReader = null!;
        private PipeWriter _pipeWriter = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerFactory = Substitute.For<ILoggerFactory>();
            _raidoOptions = Options.Create(new RaidoOptions());
            _lifetimeManager = Substitute.For<IRaidoLifetimeManager>();
            _dispatcher = Substitute.For<IRaidoDispatcher>();
            _meterFactory = Substitute.For<IMeterFactory>();
            var meter = new Meter("Raido.Server.Tests");
            _meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
            _metrics = new RaidoMetrics(_meterFactory);

            _connectionHandler = new RaidoConnectionHandler(
                _loggerFactory,
                _raidoOptions,
                _lifetimeManager,
                _dispatcher,
                _metrics);

            _connectionContext = new DefaultConnectionContext();
            var transport = Substitute.For<IDuplexPipe>();
            _pipeReader = Substitute.For<PipeReader>();
            _pipeWriter = Substitute.For<PipeWriter>();
            transport.Input.Returns(_pipeReader);
            transport.Output.Returns(_pipeWriter);
            _connectionContext.Transport = transport;
            _connection = new RaidoConnectionContext(_connectionContext, new RaidoConnectionContextOptions(), _loggerFactory);
        }

        [TestMethod]
        public async Task ConnectAsync_ShouldCallOnConnectedAsyncOnLifetimeManagerAndDispatcher()
        {
            // Arrange
            _pipeReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(), true, true)));

            // Act
            await _connectionHandler.ConnectAsync(_connection);

            // Assert
            await _lifetimeManager.Received(1).OnConnectedAsync(_connection);
            await _dispatcher.Received(1).OnConnectedAsync(_connection);
            await _lifetimeManager.Received(1).OnDisconnectedAsync(_connection);
        }

        [TestMethod]
        public async Task OnDisconnectedAsync_WhenDispatcherThrows_ShouldPropagateException()
        {
            // Arrange
            var ex = new InvalidOperationException("Dispatcher disconnect failed");
            _dispatcher.OnDisconnectedAsync(_connection, null).Returns(Task.FromException(ex));

            // Act & Assert
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _connectionHandler.OnDisconnectedAsync(_connection, null));
        }

        [TestMethod]
        public async Task ConnectAsync_WhenLifetimeManagerThrows_ShouldDisconnect()
        {
            // Arrange
            var ex = new InvalidOperationException("Lifetime manager failed");
            _lifetimeManager.OnConnectedAsync(_connection).Returns(Task.FromException(ex));

            // Act & Assert
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _connectionHandler.ConnectAsync(_connection));
            await _lifetimeManager.Received(1).OnDisconnectedAsync(_connection);
            await _dispatcher.DidNotReceive().OnConnectedAsync(_connection);
        }

        [TestMethod]
        public async Task ConnectAsync_WhenDispatcherThrows_ShouldDisconnect()
        {
            // Arrange
            var ex = new InvalidOperationException("Dispatcher failed");
            _dispatcher.OnConnectedAsync(_connection).Returns(Task.FromException(ex));
            _pipeReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(), true, true)));

            // Act
            await _connectionHandler.ConnectAsync(_connection);

            // Assert
            await _lifetimeManager.Received(1).OnConnectedAsync(_connection);
            await _dispatcher.Received(1).OnDisconnectedAsync(_connection, ex);
            await _lifetimeManager.Received(1).OnDisconnectedAsync(_connection);
        }

        [TestMethod]
        public async Task DispatchMessagesAsync_ShouldReadAndDispatchMessages()
        {
            // Arrange
            var message = new TestMessage();
            _connection.Protocol = new TestProtocol { MessageToReturn = message };
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 });

            _pipeReader.ReadAsync(Arg.Any<CancellationToken>())
                .Returns(
                    new ValueTask<ReadResult>(new ReadResult(buffer, false, false)),
                    new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(), true, false))
                );

            // Act
            await _connectionHandler.DispatchMessagesAsync(_connection);

            // Assert
            await _dispatcher.Received(1).DispatchMessageAsync(_connection, message);
        }

        [TestMethod]
        public async Task RunAsync_WhenDispatcherThrows_ShouldDisconnect()
        {
            // Arrange
            var message = new TestMessage();
            var ex = new InvalidOperationException("Dispatch failed");
            _connection.Protocol = new TestProtocol { MessageToReturn = message };
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 });

            _pipeReader.ReadAsync(Arg.Any<CancellationToken>())
                .Returns(new ValueTask<ReadResult>(new ReadResult(buffer, false, false)));

            _dispatcher.DispatchMessageAsync(_connection, message).Returns(Task.FromException(ex));

            // Act
            await _connectionHandler.RunAsync(_connection);

            // Assert
            await _dispatcher.Received(1).OnDisconnectedAsync(_connection, ex);
        }

        [TestMethod]
        public async Task ConnectAsync_WhenDispatchFails_DisconnectsTheDispatcherAndLifetimeOnce()
        {
            var message = new TestMessage();
            var exception = new InvalidOperationException("Dispatch failed");
            _connection.Protocol = new TestProtocol { MessageToReturn = message };
            _pipeReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(
                new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1 }), false, false)));
            _dispatcher.DispatchMessageAsync(_connection, message).Returns(Task.FromException(exception));

            await _connectionHandler.ConnectAsync(_connection);

            await _dispatcher.Received(1).OnDisconnectedAsync(_connection, exception);
            await _lifetimeManager.Received(1).OnDisconnectedAsync(_connection);
        }

        [TestMethod]
        public async Task DispatchMessagesAsync_WhenReadIsCanceled_ShouldStopGracefully()
        {
            // Arrange
            _connection.Protocol = new TestProtocol();

            _pipeReader.ReadAsync(Arg.Any<CancellationToken>())
                .Returns(new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(), true, false))); // IsCanceled = true

            // Act
            await _connectionHandler.DispatchMessagesAsync(_connection);

            // Assert
            await _dispatcher.DidNotReceiveWithAnyArgs().DispatchMessageAsync(Arg.Any<RaidoConnectionContext>(), Arg.Any<RaidoMessage>());
        }

        [TestMethod]
        public async Task DispatchMessagesAsync_DoesNotCompleteOwnedPipeReaderOnDispose()
        {
            var reader = new RaidoProtocolReader(_pipeReader);
            await reader.DisposeAsync();
            await _pipeReader.DidNotReceive().CompleteAsync();
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task DispatchMessagesAsync_RebindsRealPipesAndPreservesSameBufferSuffix()
        {
            using var original = new IntegrationPhysicalConnection("original");
            using var replacementPhysical = new IntegrationPhysicalConnection("replacement");
            var options = new RaidoConnectionContextOptions
            {
                StatefulReconnectEnabled = true,
                StatefulReconnectGracePeriod = TimeSpan.FromSeconds(1)
            };
            var target = new RaidoConnectionContext(original.Context, options, _loggerFactory)
            {
                Protocol = new TestProtocol { MessageToReturn = new TestMessage() }
            };
            target.Features.Get<IRaidoStatefulReconnectFeature>()!.EnableReconnect();
            var store = new RaidoConnectionStore();
            store.Add(target);

            var replacementApplication = new RaidoApplicationConnection();
            var replacementSession = new RaidoPhysicalConnectionSession(replacementPhysical.Context, _loggerFactory);
            var replacement = new RaidoConnectionContext(
                replacementApplication,
                replacementSession,
                replacementPhysical.Context.Features,
                replacementPhysical.Context.Items,
                options,
                _loggerFactory)
            {
                Protocol = new SingleByteProtocol()
            };
            var gameProtocol = new TestProtocol();
            var dispatchedGameMessages = 0;
            _dispatcher.DispatchMessageAsync(Arg.Any<RaidoConnectionContext>(), Arg.Any<RaidoMessage>())
                .Returns(callInfo =>
                {
                    var connection = callInfo.Arg<RaidoConnectionContext>();
                    if (ReferenceEquals(connection, replacement))
                    {
                        return store.TryRebindAsync(target.ConnectionId, replacement, gameProtocol).AsTask();
                    }

                    Interlocked.Increment(ref dispatchedGameMessages);
                    return Task.CompletedTask;
                });

            var targetTask = _connectionHandler.DispatchMessagesAsync(target);
            original.Closed.Cancel();
            await WaitUntilAsync(() => target.LifecycleState == RaidoConnectionLifecycleState.Reconnecting);

            var physicalTask = replacementSession.RunAsync(replacement, replacementApplication);
            var replacementTask = _connectionHandler.DispatchMessagesAsync(replacement);
            replacementPhysical.Input.Writer.Write(new byte[] { 1, 2 });
            await replacementPhysical.Input.Writer.FlushAsync();

            await replacementTask;
            await WaitUntilAsync(() => Volatile.Read(ref dispatchedGameMessages) == 1);
            Assert.AreEqual(1, dispatchedGameMessages);
            Assert.AreEqual(RaidoConnectionLifecycleState.Connected, target.LifecycleState);
            Assert.IsFalse(physicalTask.IsCompleted, "The physical handler must remain alive after logical application transfer.");

            target.Abort();
            await targetTask;
            await physicalTask;
            store.Dispose();
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                await Task.Delay(1);
            }

            Assert.IsTrue(condition());
        }

        private sealed class IntegrationPhysicalConnection : IDisposable
        {
            public readonly CancellationTokenSource Closed = new();
            public readonly Pipe Input = new();
            public readonly Pipe Output = new();
            public readonly ConnectionContext Context;

            public IntegrationPhysicalConnection(string id)
            {
                var transport = Substitute.For<IDuplexPipe>();
                transport.Input.Returns(Input.Reader);
                transport.Output.Returns(Output.Writer);
                Context = Substitute.For<ConnectionContext>();
                Context.ConnectionId.Returns(id);
                Context.Transport.Returns(transport);
                Context.Features.Returns(new FeatureCollection());
                Context.Items.Returns(new Dictionary<object, object?>());
                Context.ConnectionClosed.Returns(Closed.Token);
            }

            public void Dispose()
            {
                Closed.Cancel();
                Input.Reader.Complete();
                Output.Reader.Complete();
                Closed.Dispose();
            }
        }

        private sealed class SingleByteProtocol : IRaidoProtocol
        {
            private bool _first = true;
            public string Name => "single-byte";
            public int Version => 1;
            public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage message)
            {
                if (_first)
                {
                    if (input.Length < 1)
                    {
                        consumed = input.Start;
                        examined = input.End;
                        message = null!;
                        return false;
                    }

                    _first = false;
                    consumed = input.GetPosition(1);
                    examined = input.End;
                    message = new TestMessage();
                    return true;
                }

                consumed = input.End;
                examined = input.End;
                message = new TestMessage();
                return true;
            }

            public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output) { }
            public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => ReadOnlyMemory<byte>.Empty;
            public bool IsVersionSupported(int version) => version == 1;
        }
    }
}
