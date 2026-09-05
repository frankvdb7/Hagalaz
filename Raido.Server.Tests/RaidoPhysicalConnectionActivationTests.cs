using System.Buffers;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Protocol;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoPhysicalConnectionActivationTests
{
    [TestMethod]
    public void PublicRaidoHubApis_DoNotExposePhysicalConnectionReplacement()
    {
        var forbiddenNames = new[]
        {
            "TryActivatePhysicalConnection",
            "TryAttachPhysicalConnection",
            "TryReconnect",
            "ResumePhysicalConnection",
            "ReplaceTransport"
        };

        foreach (var type in new[] { typeof(RaidoHubConnectionContext), typeof(RaidoHubConnectionHandler) })
        {
            foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
            {
                Assert.IsFalse(forbiddenNames.Contains(method.Name));
                Assert.IsFalse(method.GetParameters().Any(parameter =>
                    typeof(ConnectionContext).IsAssignableFrom(parameter.ParameterType)));
            }
        }
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task Dispatcher_AttachesToExistingLogicalConnectionAndReadsBufferedInput()
    {
        using var fixture = CreateFixture();
        var initial = CreatePhysicalConnection("physical-a", out var initialInput, out var initialOutput, out var initialClosed);
        var replacement = CreatePhysicalConnection("physical-b", out var replacementInput, out var replacementOutput, out var replacementClosed);
        try
        {
            Assert.IsTrue(fixture.Tcp.TryAttachPhysicalConnection(initial));
            var logical = CreateLogicalConnection(fixture.Tcp, fixture.Options);
            fixture.Tcp.OnPhysicalConnectionClosed(initial);
            Assert.IsTrue(fixture.Tcp.Transport.Input.TryRead(out var boundary));
            Assert.IsTrue(boundary.IsCanceled);
            Assert.IsTrue(boundary.Buffer.IsEmpty);
            fixture.Tcp.Transport.Input.AdvanceTo(boundary.Buffer.End);
            fixture.Tcp.AcknowledgeInputBoundary();
            await replacementInput.Writer.WriteAsync(new byte[] { 7, 8 });

            var dispatcher = CreateDispatcher(fixture, logical);
            await dispatcher.OnConnectedAsync(replacement);
            Assert.IsTrue(fixture.Tcp.TryGetCurrentConnection(out var current));
            Assert.AreSame(replacement, current);
            var buffered = await fixture.Tcp.Transport.Input.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            CollectionAssert.AreEqual(new byte[] { 7, 8 }, buffered.Buffer.ToArray());
            fixture.Tcp.Transport.Input.AdvanceTo(buffered.Buffer.End);
            Assert.AreEqual("physical-a", logical.ConnectionId);
            await fixture.LifetimeManager.DidNotReceiveWithAnyArgs().OnConnectedAsync(null!);
            await fixture.LifetimeManager.DidNotReceiveWithAnyArgs().OnDisconnectedAsync(null!);
            await fixture.Dispatcher.DidNotReceiveWithAnyArgs().OnConnectedAsync(null!);

            logical.Abort();
            await logical.CleanupAsync();
        }
        finally
        {
            initialClosed.Dispose();
            replacementClosed.Dispose();
            await CompleteAsync(initialInput, initialOutput, replacementInput, replacementOutput);
        }
    }

    [TestMethod]
    public async Task Dispatcher_WhenLogicalConnectionIsTerminalRejectsPhysicalConnection()
    {
        using var fixture = CreateFixture();
        var initial = CreatePhysicalConnection("physical-a", out var initialInput, out var initialOutput, out var initialClosed);
        var replacement = CreatePhysicalConnection("physical-b", out var replacementInput, out var replacementOutput, out var replacementClosed);
        try
        {
            Assert.IsTrue(fixture.Tcp.TryAttachPhysicalConnection(initial));
            var logical = CreateLogicalConnection(fixture.Tcp, fixture.Options);
            logical.Abort();

            var dispatcher = CreateDispatcher(fixture, logical);
            await dispatcher.OnConnectedAsync(replacement);
            Assert.IsFalse(fixture.Tcp.TryGetCurrentConnection(out _));
            await fixture.LifetimeManager.DidNotReceiveWithAnyArgs().OnConnectedAsync(null!);
            await fixture.LifetimeManager.DidNotReceiveWithAnyArgs().OnDisconnectedAsync(null!);
            await fixture.Dispatcher.DidNotReceiveWithAnyArgs().OnConnectedAsync(null!);

            await logical.CleanupAsync();
        }
        finally
        {
            initialClosed.Dispose();
            replacementClosed.Dispose();
            await CompleteAsync(initialInput, initialOutput, replacementInput, replacementOutput);
        }
    }

    private static Fixture CreateFixture()
    {
        var meter = new Meter($"{nameof(RaidoPhysicalConnectionActivationTests)}-{Guid.NewGuid()}");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            new RaidoMetrics(meterFactory));
        var options = new RaidoConnectionContextOptions
        {
            StatefulReconnectEnabled = true,
            StatefulReconnectTimeout = TimeSpan.FromSeconds(5)
        };
        return new Fixture(
            handler,
            new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance),
            options,
            lifetimeManager,
            dispatcher,
            meter);
    }

    private static RaidoConnectionDispatcher CreateDispatcher(Fixture fixture, RaidoHubConnectionContext logical) =>
        new(
            (_, _) => new ValueTask<RaidoConnectionSelection>(RaidoConnectionSelection.Existing(logical)),
            Substitute.For<IRaidoHubConnectionContextFactory>(),
            fixture.Handler,
            NullLogger<RaidoConnectionDispatcher>.Instance);

    private static RaidoHubConnectionContext CreateLogicalConnection(
        RaidoTcpConnectionContext tcp,
        RaidoConnectionContextOptions options) =>
        new(tcp, options, Substitute.For<IRaidoProtocol>(), NullLoggerFactory.Instance, TimeProvider.System);

    private static ConnectionContext CreatePhysicalConnection(
        string connectionId,
        out Pipe input,
        out Pipe output,
        out CancellationTokenSource closed)
    {
        input = new Pipe();
        output = new Pipe();
        closed = new CancellationTokenSource();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns(connectionId);
        connection.Features.Returns(new FeatureCollection());
        connection.Transport.Returns(transport);
        connection.ConnectionClosed.Returns(closed.Token);
        return connection;
    }

    private static async Task CompleteAsync(
        Pipe initialInput,
        Pipe initialOutput,
        Pipe replacementInput,
        Pipe replacementOutput)
    {
        await initialInput.Reader.CompleteAsync();
        await initialInput.Writer.CompleteAsync();
        await initialOutput.Reader.CompleteAsync();
        await initialOutput.Writer.CompleteAsync();
        await replacementInput.Reader.CompleteAsync();
        await replacementInput.Writer.CompleteAsync();
        await replacementOutput.Reader.CompleteAsync();
        await replacementOutput.Writer.CompleteAsync();
    }

    private sealed class Fixture(
        RaidoHubConnectionHandler handler,
        RaidoTcpConnectionContext tcp,
        RaidoConnectionContextOptions options,
        IRaidoHubLifetimeManager lifetimeManager,
        IRaidoDispatcher dispatcher,
        Meter meter) : IDisposable
    {
        public RaidoHubConnectionHandler Handler { get; } = handler;
        public RaidoTcpConnectionContext Tcp { get; } = tcp;
        public RaidoConnectionContextOptions Options { get; } = options;
        public IRaidoHubLifetimeManager LifetimeManager { get; } = lifetimeManager;
        public IRaidoDispatcher Dispatcher { get; } = dispatcher;

        public void Dispose() => meter.Dispose();
    }
}
