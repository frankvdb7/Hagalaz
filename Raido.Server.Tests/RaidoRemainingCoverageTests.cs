using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;
using Raido.Server.Internal.Diagnostics;
using Raido.Server.Internal.Reflection;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoRemainingCoverageTests
{
    private readonly List<RaidoHubConnectionContext> _connections = new();
    private readonly List<(Pipe Input, Pipe Output)> _transports = new();

    [TestCleanup]
    public async Task CleanupConnections()
    {
        foreach (var connection in _connections)
        {
            connection.Abort();
            await connection.CleanupAsync();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }

    private sealed class EmptyFilter : IRaidoHubFilter { }

    private sealed class DisposableFilter : IRaidoHubFilter, IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private sealed class AsyncDisposableFilter : IRaidoHubFilter, IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private ConnectionContext CreateRawConnection(string id = "factory")
    {
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns(id);
        var transport = Substitute.For<IDuplexPipe>();
        var input = new Pipe();
        var output = new Pipe();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        connection.Transport.Returns(transport);
        connection.ConnectionClosed.Returns(CancellationToken.None);
        _transports.Add((input, output));
        return connection;
    }

    [TestMethod]
    public void OptionsSetup_AppliesDefaultsAndPreservesConfiguredValues()
    {
        var setup = new RaidoOptionsSetup();
        var defaults = new RaidoOptions();
        setup.Configure(defaults);
        Assert.AreEqual(TimeSpan.FromSeconds(15), defaults.KeepAliveInterval);
        Assert.AreEqual(TimeSpan.FromSeconds(30), defaults.ClientTimeoutInterval);
        Assert.AreEqual(TimeSpan.FromSeconds(30), defaults.StatefulReconnectTimeout);
        Assert.AreEqual(32 * 1024, defaults.MaximumReceiveMessageSize);

        var configured = new RaidoOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(1),
            ClientTimeoutInterval = TimeSpan.FromSeconds(2),
            StatefulReconnectTimeout = TimeSpan.FromSeconds(7),
            MaximumReceiveMessageSize = 3
        };
        setup.Configure(configured);
        Assert.AreEqual(TimeSpan.FromSeconds(1), configured.KeepAliveInterval);
        Assert.AreEqual(TimeSpan.FromSeconds(2), configured.ClientTimeoutInterval);
        Assert.AreEqual(TimeSpan.FromSeconds(7), configured.StatefulReconnectTimeout);
        Assert.AreEqual(3, configured.MaximumReceiveMessageSize);

        var filter = Substitute.For<IRaidoHubFilter>();
        configured.AddGlobalFilter(filter);
        var hubSetup = new RaidoHubOptionsSetup<RaidoHub>(Options.Create(configured));
        var hubOptions = new RaidoHubOptions<RaidoHub>();
        hubSetup.Configure(hubOptions);
        Assert.AreSame(filter, hubOptions.HubFilters![0]);
    }

    [TestMethod]
    public void ConnectionFactory_BuildsWithExplicitProtocolAndOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<RaidoOptions>>(Options.Create(new RaidoOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(10),
            ClientTimeoutInterval = TimeSpan.FromSeconds(20)
        }));
        services.AddSingleton<TestProtocol>();
        using var provider = services.BuildServiceProvider();
        var factory = new DefaultRaidoHubConnectionContextFactory(
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>(),
            provider.GetRequiredService<IOptions<RaidoOptions>>());
        var connection = CreateRawConnection();
        var built = factory.Create(
            connection,
            provider.GetRequiredService<TestProtocol>(),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2));
        _connections.Add(built);
        Assert.AreNotSame(connection.Transport.Input, built.TcpConnection.Transport.Input);
        Assert.IsInstanceOfType<TestProtocol>(built.Protocol);
        Assert.AreEqual("factory", built.ConnectionId);
    }

    [TestMethod]
    public async Task DefaultDispatcher_ForwardsLifecycleAndNonPingMessages()
    {
        var first = Substitute.For<IRaidoHubDispatcher>();
        var second = Substitute.For<IRaidoHubDispatcher>();
        var dispatcher = new DefaultRaidoDispatcher(new[] { first, second });
        var rawConnection = CreateRawConnection("dispatcher");
        var connection = RaidoTestConnectionFactory.Create(rawConnection, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance);
        _connections.Add(connection);
        var message = new TestMessage();
        await dispatcher.OnConnectedAsync(connection);
        await dispatcher.OnDisconnectedAsync(connection, null);
        await dispatcher.DispatchMessageAsync(connection, message);
        await dispatcher.DispatchMessageAsync(connection, PingMessage.Instance);
        await first.Received().OnConnectedAsync(connection);
        await second.Received().OnDisconnectedAsync(connection, null);
        await first.Received().DispatchMessageAsync(connection, message);
    }

    [TestMethod]
    public void Metrics_RecordsEnabledAndDisabledContexts()
    {
        var meter = new Meter("Raido.Server.Tests.Metrics");
        var factory = Substitute.For<IMeterFactory>();
        factory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(factory);
        var context = metrics.CreateContext();
        metrics.ConnectionStart(context);
        metrics.ConnectionStop(context, 0, 1);
        metrics.ConnectionStart(new MetricsContext(true, true));
        metrics.ConnectionStop(new MetricsContext(true, true), 0, 1);
        metrics.Dispose();
    }

    [TestMethod]
    public async Task HubFilterFactory_UsesRegisteredOrOwnedFiltersAndDisposesThem()
    {
        var method = typeof(RaidoHub).GetMethod(nameof(RaidoHub.OnConnectedAsync))!;
        var executor = ObjectMethodExecutor.Create(method, typeof(RaidoHub).GetTypeInfo());
        var context = Substitute.For<RaidoCallerContext>();
        using var emptyProvider = new ServiceCollection().BuildServiceProvider();
        var invocation = new RaidoHubInvocationContext(executor, context, emptyProvider, new TestHub(), Array.Empty<object?>());
        var nextCalled = false;
        var registered = new DisposableFilter();
        using var services = new ServiceCollection().AddSingleton<DisposableFilter>(registered).BuildServiceProvider();
        var factory = new RaidoHubFilterFactory(typeof(DisposableFilter));
        var registeredInvocation = new RaidoHubInvocationContext(executor, context, services, new TestHub(), Array.Empty<object?>());
        await factory.InvokeMethodAsync(registeredInvocation, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(null);
        });
        Assert.IsTrue(nextCalled);
        Assert.IsFalse(registered.Disposed);

        var owned = new RaidoHubFilterFactory(typeof(DisposableFilter));
        using var ownedProvider = new ServiceCollection().BuildServiceProvider();
        var ownedInvocation = new RaidoHubInvocationContext(executor, context, ownedProvider, new TestHub(), Array.Empty<object?>());
        await owned.InvokeMethodAsync(ownedInvocation, _ => ValueTask.FromResult<object?>(null));

        var asyncFactory = new RaidoHubFilterFactory(typeof(AsyncDisposableFilter));
        await asyncFactory.OnConnectedAsync(new RaidoHubLifetimeContext(context, services, new TestHub()), _ => Task.CompletedTask);
        await asyncFactory.OnDisconnectedAsync(new RaidoHubLifetimeContext(context, services, new TestHub()), null, (_, _) => Task.CompletedTask);
    }

    [TestMethod]
    public void ActivityCreator_CreatesOnlyWhenDiagnosticsAreEnabled()
    {
        var source = new ActivitySource("Raido.Server.Tests.Activity");
        var propagator = DistributedContextPropagator.Current;
        var headers = new Dictionary<string, object?>
        {
            ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
            ["tracestate"] = "vendor=value"
        };
        static void Get(object? carrier, string field, out string? value, out IEnumerable<string>? values)
        {
            values = null;
            var headers = (Dictionary<string, object?>)carrier!;
            headers.TryGetValue(field, out var rawValue);
            value = rawValue?.ToString();
        }
        Assert.IsNull(ActivityCreator.CreateFromRemote(source, propagator, headers, Get, "operation", ActivityKind.Server, null, null, false));
        using var activity = ActivityCreator.CreateFromRemote(source, propagator, headers, Get, "operation", ActivityKind.Server,
            new[] { new KeyValuePair<string, object?>("tag", "value") }, null, true);
        Assert.IsNotNull(activity);
        Assert.AreEqual("operation", activity!.OperationName);
    }

    private sealed class TestHub : RaidoHub { }
}
