using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
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
            transport.Input.Returns(_pipeReader);
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
    }
}