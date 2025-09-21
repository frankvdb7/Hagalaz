using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Server.Internal;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Buffers;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace Raido.Server.Tests
{
    [TestClass]
    public class RaidoConnectionHandlerTests
    {
        [TestMethod]
        public async Task ConnectAsync_ShouldCallOnConnectedAsyncOnLifetimeManagerAndDispatcher()
        {
            // Arrange
            var lifetimeManager = Substitute.For<IRaidoLifetimeManager>();
            var dispatcher = Substitute.For<IRaidoDispatcher>();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var meterFactory = Substitute.For<IMeterFactory>();
            var meter = new Meter("Raido.Server.Tests");
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);

            var connectionHandler = new RaidoConnectionHandler(
                loggerFactory,
                Options.Create(new RaidoOptions()),
                lifetimeManager,
                dispatcher,
                new RaidoMetrics(meterFactory));

            var connectionContext = new DefaultConnectionContext();
            var pipeReader = Substitute.For<PipeReader>();
            var transport = Substitute.For<IDuplexPipe>();
            transport.Input.Returns(pipeReader);
            connectionContext.Transport = transport;

            var connection = new RaidoConnectionContext(connectionContext, new RaidoConnectionContextOptions(), loggerFactory);

            pipeReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(), true, true)));

            // Act
            await connectionHandler.ConnectAsync(connection);

            // Assert
            await lifetimeManager.Received(1).OnConnectedAsync(connection);
            await dispatcher.Received(1).OnConnectedAsync(connection);
            await lifetimeManager.Received(1).OnDisconnectedAsync(connection);
        }

        [TestMethod]
        public async Task OnDisconnectedAsync_ShouldCallOnDisconnectedAsyncOnDispatcher()
        {
            // Arrange
            var lifetimeManager = Substitute.For<IRaidoLifetimeManager>();
            var dispatcher = Substitute.For<IRaidoDispatcher>();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var meterFactory = Substitute.For<IMeterFactory>();
            var meter = new Meter("Raido.Server.Tests");
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);

            var connectionHandler = new RaidoConnectionHandler(
                loggerFactory,
                Options.Create(new RaidoOptions()),
                lifetimeManager,
                dispatcher,
                new RaidoMetrics(meterFactory));

            var connectionContext = new DefaultConnectionContext();
            var transport = Substitute.For<IDuplexPipe>();
            var pipeReader = Substitute.For<PipeReader>();
            var pipeWriter = Substitute.For<PipeWriter>();
            transport.Input.Returns(pipeReader);
            transport.Output.Returns(pipeWriter);
            connectionContext.Transport = transport;
            var connection = new RaidoConnectionContext(connectionContext, new RaidoConnectionContextOptions(), loggerFactory);

            // Act
            await connectionHandler.OnDisconnectedAsync(connection, null);

            // Assert
            await dispatcher.Received(1).OnDisconnectedAsync(connection, null);
        }
    }
}
