using System.Buffers;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
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

        foreach (var type in new[]
                 {
                     typeof(RaidoHubConnectionContext),
                     typeof(RaidoHubConnectionHandler),
                     typeof(RaidoConnectionDispatchContext)
                 })
        {
            foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
            {
                Assert.IsFalse(forbiddenNames.Contains(method.Name));
                Assert.IsFalse(method.GetParameters().Any(parameter =>
                    typeof(ConnectionContext).IsAssignableFrom(parameter.ParameterType)));
            }
        }

        Assert.AreEqual(0, typeof(RaidoConnectionDispatchContext)
            .GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Length);
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

            var (dispatcher, dispatcherProvider) = CreateDispatcher(fixture, logical);
            using (dispatcherProvider)
            {
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
            }

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

            var (dispatcher, dispatcherProvider) = CreateDispatcher(fixture, logical);
            using (dispatcherProvider)
            {
            await dispatcher.OnConnectedAsync(replacement);
            Assert.IsFalse(fixture.Tcp.TryGetCurrentConnection(out _));
            await fixture.LifetimeManager.DidNotReceiveWithAnyArgs().OnConnectedAsync(null!);
            await fixture.LifetimeManager.DidNotReceiveWithAnyArgs().OnDisconnectedAsync(null!);
            await fixture.Dispatcher.DidNotReceiveWithAnyArgs().OnConnectedAsync(null!);
            }

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
    public async Task Dispatcher_WhenExistingTargetIsActiveRejectsBeforePreparation()
    {
        using var fixture = CreateFixture();
        var initial = CreatePhysicalConnection("physical-a", out var initialInput, out var initialOutput, out var initialClosed);
        var replacement = CreatePhysicalConnection("physical-b", out var replacementInput, out var replacementOutput, out var replacementClosed);
        var prepared = 0;
        try
        {
            Assert.IsTrue(fixture.Tcp.TryAttachPhysicalConnection(initial));
            var oldProtocol = Substitute.For<IRaidoProtocol>();
            var logical = new RaidoHubConnectionContext(
                fixture.Tcp,
                fixture.Options,
                oldProtocol,
                NullLoggerFactory.Instance,
                TimeProvider.System);

            var (dispatcher, dispatcherProvider) = CreateDispatcher(
                fixture,
                logical,
                (_, dispatch, cancellationToken) =>
                    dispatch.DispatchExistingAsync(
                        logical,
                        _ =>
                        {
                            Interlocked.Increment(ref prepared);
                            return ValueTask.CompletedTask;
                        },
                        cancellationToken).AsTask());
            using (dispatcherProvider)
            {
                await dispatcher.OnConnectedAsync(replacement);
            }

            Assert.AreEqual(0, prepared);
            Assert.AreSame(oldProtocol, logical.Protocol);
            Assert.IsFalse(logical.IsTerminal);
            Assert.IsTrue(fixture.Tcp.TryGetCurrentConnection(out var current));
            Assert.AreSame(initial, current);
            replacement.Received(1).Abort(Arg.Any<ConnectionAbortedException>());

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
    public async Task Dispatcher_WhenPreparationFailsLeavesReconnectableTargetAlive()
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
            fixture.Tcp.Transport.Input.AdvanceTo(boundary.Buffer.End);
            fixture.Tcp.AcknowledgeInputBoundary();

            var (dispatcher, dispatcherProvider) = CreateDispatcher(
                fixture,
                logical,
                (_, dispatch, cancellationToken) => dispatch.DispatchExistingAsync(
                    logical,
                    static _ => ValueTask.FromException(new OperationCanceledException()),
                    cancellationToken).AsTask());
            using (dispatcherProvider)
            {
                await dispatcher.OnConnectedAsync(replacement);
            }

            Assert.IsFalse(logical.IsTerminal);
            Assert.IsFalse(fixture.Tcp.TryGetCurrentConnection(out _));
            replacement.Received(1).Abort(Arg.Any<ConnectionAbortedException>());

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
    public async Task DispatchContext_CannotDispatchOnePhysicalConnectionTwice()
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
            fixture.Tcp.Transport.Input.AdvanceTo(boundary.Buffer.End);
            fixture.Tcp.AcknowledgeInputBoundary();

            var dispatch = new RaidoConnectionDispatchContext(
                replacement,
                Substitute.For<IRaidoHubConnectionContextFactory>(),
                fixture.Handler);
            Assert.IsTrue(await dispatch.DispatchExistingAsync(
                logical,
                static _ => ValueTask.CompletedTask,
                CancellationToken.None));

            var threw = false;
            try
            {
                await dispatch.DispatchExistingAsync(
                    logical,
                    static _ => ValueTask.CompletedTask,
                    CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            Assert.IsTrue(threw);

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
    public async Task Dispatcher_CreatesAndDisposesAnIndependentApplicationScopePerConnection()
    {
        var states = new List<ScopedConnectionState>();
        var services = new ServiceCollection();
        services.AddScoped<ScopedConnectionState>();
        services.AddScoped<RaidoConnectionDelegate>(provider =>
        {
            var state = provider.GetRequiredService<ScopedConnectionState>();
            states.Add(state);
            return (_, _, _) =>
            {
                state.Used++;
                return Task.CompletedTask;
            };
        });
        await using var provider = services.BuildServiceProvider();
        using var fixture = CreateFixture();
        var first = CreatePhysicalConnection("physical-a", out var firstInput, out var firstOutput, out var firstClosed);
        var second = CreatePhysicalConnection("physical-b", out var secondInput, out var secondOutput, out var secondClosed);
        var dispatcher = new RaidoConnectionDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<IRaidoHubConnectionContextFactory>(),
            fixture.Handler,
            NullLogger<RaidoConnectionDispatcher>.Instance);

        try
        {
            await dispatcher.OnConnectedAsync(first);
            await dispatcher.OnConnectedAsync(second);

            Assert.AreEqual(2, states.Count);
            Assert.AreNotSame(states[0], states[1]);
            Assert.AreEqual(1, states[0].Used);
            Assert.AreEqual(1, states[1].Used);
            Assert.IsTrue(states[0].Disposed);
            Assert.IsTrue(states[1].Disposed);
        }
        finally
        {
            firstClosed.Dispose();
            secondClosed.Dispose();
            await CompleteAsync(firstInput, firstOutput, secondInput, secondOutput);
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

    private static (RaidoConnectionDispatcher Dispatcher, ServiceProvider Provider) CreateDispatcher(
        Fixture fixture,
        RaidoHubConnectionContext logical,
        RaidoConnectionDelegate? application = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<RaidoConnectionDelegate>((_) => application ??
            ((_, dispatch, cancellationToken) =>
                dispatch.DispatchExistingAsync(
                    logical,
                    static _ => ValueTask.CompletedTask,
                    cancellationToken).AsTask()));
        var provider = services.BuildServiceProvider();
        return (
            new RaidoConnectionDispatcher(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<IRaidoHubConnectionContextFactory>(),
                fixture.Handler,
                NullLogger<RaidoConnectionDispatcher>.Instance),
            provider);
    }

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

    private sealed class ScopedConnectionState : IDisposable
    {
        public int Used { get; set; }
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
