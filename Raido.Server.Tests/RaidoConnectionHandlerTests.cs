using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Internal;
using System.Buffers;
using System.Diagnostics.Metrics;

namespace Raido.Server.Tests
{
    [TestClass]
    public class RaidoConnectionHandlerTests
    {
        private class TestLoggerFactory : ILoggerFactory
        {
            public void Dispose() { }
            public ILogger CreateLogger(string categoryName) => new TestLogger();
            public void AddProvider(ILoggerProvider provider) { }
        }

        private class TestLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }

        private class TestMeterFactory : IMeterFactory
        {
            public Meter Create(MeterOptions options) => new Meter(options);
            public void Dispose() { }
        }

        private class TestRaidoLifetimeManager : IRaidoLifetimeManager
        {
            public bool OnConnectedAsyncCalled { get; private set; }
            public bool OnDisconnectedAsyncCalled { get; private set; }

            public Task OnConnectedAsync(RaidoConnectionContext connection)
            {
                OnConnectedAsyncCalled = true;
                return Task.CompletedTask;
            }

            public Task OnDisconnectedAsync(RaidoConnectionContext connection)
            {
                OnDisconnectedAsyncCalled = true;
                return Task.CompletedTask;
            }

            public Task SendAllAsync(RaidoMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task SendAllExceptAsync(RaidoMessage message, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task SendConnectionAsync(RaidoMessage message, string connectionId, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task SendConnectionsAsync(RaidoMessage message, IReadOnlyList<string> connectionIds, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private class TestRaidoDispatcher : IRaidoDispatcher
        {
            public bool OnConnectedAsyncCalled { get; private set; }
            public bool OnDisconnectedAsyncCalled { get; private set; }

            public Task OnConnectedAsync(RaidoConnectionContext connection)
            {
                OnConnectedAsyncCalled = true;
                return Task.CompletedTask;
            }

            public Task OnDisconnectedAsync(RaidoConnectionContext connection, Exception? exception)
            {
                OnDisconnectedAsyncCalled = true;
                return Task.CompletedTask;
            }

            public Task DispatchMessageAsync(RaidoConnectionContext connection, RaidoMessage message) => Task.CompletedTask;
        }

        private class TestConnectionContext : ConnectionContext
        {
            public override string ConnectionId { get; set; } = "test";
            public override IFeatureCollection Features { get; } = new FeatureCollection();
            public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();
            public override IDuplexPipe Transport { get; set; } = new DuplexPipe();
        }

        private class DuplexPipe : IDuplexPipe
        {
            public PipeReader Input { get; } = new TestPipeReader();
            public PipeWriter Output { get; } = new TestPipeWriter();
        }

        private class TestPipeReader : PipeReader
        {
            public override void AdvanceTo(SequencePosition consumed) { }
            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { }
            public override void CancelPendingRead() { }
            public override void Complete(Exception? exception = null) { }
            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                return new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(), true, true));
            }
            public override bool TryRead(out ReadResult result)
            {
                result = default;
                return false;
            }
        }

        private class TestPipeWriter : PipeWriter
        {
            public override void Advance(int bytes) { }
            public override void CancelPendingFlush() { }
            public override void Complete(Exception? exception = null) { }
            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                return new ValueTask<FlushResult>(new FlushResult());
            }
            public override Memory<byte> GetMemory(int sizeHint = 0) => new byte[sizeHint];
            public override Span<byte> GetSpan(int sizeHint = 0) => new byte[sizeHint];
        }


        [TestMethod]
        public async Task ConnectAsync_ShouldCallOnConnectedAsyncOnLifetimeManagerAndDispatcher()
        {
            // Arrange
            var lifetimeManager = new TestRaidoLifetimeManager();
            var dispatcher = new TestRaidoDispatcher();
            var connectionHandler = new RaidoConnectionHandler(
                new TestLoggerFactory(),
                Options.Create(new RaidoOptions()),
                lifetimeManager,
                dispatcher,
                new RaidoMetrics(new TestMeterFactory()));
            var connectionContext = new TestConnectionContext();
            var connection = new RaidoConnectionContext(connectionContext, new RaidoConnectionContextOptions(), new TestLoggerFactory());

            // Act
            await connectionHandler.ConnectAsync(connection);

            // Assert
            Assert.IsTrue(lifetimeManager.OnConnectedAsyncCalled);
            Assert.IsTrue(dispatcher.OnConnectedAsyncCalled);
        }

        [TestMethod]
        public async Task OnDisconnectedAsync_ShouldCallOnDisconnectedAsyncOnDispatcher()
        {
            // Arrange
            var lifetimeManager = new TestRaidoLifetimeManager();
            var dispatcher = new TestRaidoDispatcher();
            var connectionHandler = new RaidoConnectionHandler(
                new TestLoggerFactory(),
                Options.Create(new RaidoOptions()),
                lifetimeManager,
                dispatcher,
                new RaidoMetrics(new TestMeterFactory()));
            var connectionContext = new TestConnectionContext();
            var connection = new RaidoConnectionContext(connectionContext, new RaidoConnectionContextOptions(), new TestLoggerFactory());

            // Act
            await connectionHandler.OnDisconnectedAsync(connection, null);

            // Assert
            Assert.IsTrue(dispatcher.OnDisconnectedAsyncCalled);
        }
    }
}
