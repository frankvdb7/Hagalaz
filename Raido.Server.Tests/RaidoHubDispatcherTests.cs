using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;

namespace Raido.Server.Tests;

[TestClass]
[DoNotParallelize]
public sealed class RaidoHubDispatcherTests
{
    private sealed class DispatchMessage : RaidoMessage { }
    private sealed class OtherMessage : RaidoMessage { }
    private sealed class TaskMessage : RaidoMessage { }
    private sealed class ValueTaskMessage : RaidoMessage { }

    private sealed class DispatchHub : RaidoHub
    {
        public static int Connected;
        public static int Disconnected;
        public static int Invoked;
        public static int FilterInvoked;
        public static int AuthorizedInvoked;
        public static int TaskInvoked;
        public static int ValueTaskInvoked;
        public static bool ThrowFromHandler;

        public override Task OnConnectedAsync()
        {
            Connected++;
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Disconnected++;
            return Task.CompletedTask;
        }

        [RaidoMessageHandler(typeof(DispatchMessage))]
        public DispatchMessage Handle(DispatchMessage message)
        {
            Invoked++;
            if (ThrowFromHandler)
            {
                throw new InvalidOperationException("handler failure");
            }
            return message;
        }

        [Authorize]
        [RaidoMessageHandler(typeof(OtherMessage))]
        public Task<OtherMessage> Authorized(OtherMessage message)
        {
            AuthorizedInvoked++;
            return Task.FromResult(message);
        }

        [RaidoMessageHandler(typeof(TaskMessage))]
        public Task HandleTask(TaskMessage message)
        {
            TaskInvoked++;
            return Task.CompletedTask;
        }

        [RaidoMessageHandler(typeof(ValueTaskMessage))]
        public ValueTask<DispatchMessage> HandleValueTask(ValueTaskMessage message)
        {
            ValueTaskInvoked++;
            return ValueTask.FromResult(new DispatchMessage());
        }
    }

    private sealed class DispatchFilter : IRaidoHubFilter
    {
        public ValueTask<object?> InvokeMethodAsync(RaidoHubInvocationContext context, Func<RaidoHubInvocationContext, ValueTask<object?>> next)
        {
            DispatchHub.FilterInvoked++;
            return next(context);
        }

        public Task OnConnectedAsync(RaidoHubLifetimeContext context, Func<RaidoHubLifetimeContext, Task> next) => next(context);
        public Task OnDisconnectedAsync(RaidoHubLifetimeContext context, Exception? exception, Func<RaidoHubLifetimeContext, Exception?, Task> next) => next(context, exception);
    }

    private static (ServiceProvider Provider, IRaidoContext Context) CreateProvider(bool withFilter = false)
    {
        var services = new ServiceCollection();
        var context = Substitute.For<IRaidoContext>();
        context.Clients.Returns(Substitute.For<IRaidoClients>());
        services.AddSingleton(context);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        services.AddAuthorization();
        services.AddSingleton(new RaidoServerActivitySource());
        services.AddScoped<IRaidoCallerContextAccessor, DefaultRaidoCallerContextAccessor>();
        services.AddScoped<IRaidoHubActivator<DispatchHub>, DefaultRaidoHubActivator<DispatchHub>>();
        services.AddOptions<RaidoHubOptions<DispatchHub>>();
        if (withFilter)
        {
            services.Configure<RaidoHubOptions<DispatchHub>>(options => options.AddFilter(new DispatchFilter()));
        }
        return (services.BuildServiceProvider(), context);
    }

    private static RaidoConnectionContext CreateConnection(string id = "connection")
    {
        var context = Substitute.For<ConnectionContext>();
        context.ConnectionId.Returns(id);
        var transport = Substitute.For<IDuplexPipe>();
        var input = new Pipe();
        var output = new Pipe();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        context.Transport.Returns(transport);
        context.Features.Returns(new FeatureCollection());
        context.ConnectionClosed.Returns(CancellationToken.None);
        return new RaidoConnectionContext(context, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance)
        {
            Protocol = new TestProtocol()
        };
    }

    private static DefaultRaidoHubDispatcher<DispatchHub> CreateDispatcher(ServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<RaidoHubOptions<DispatchHub>>>();
        return new DefaultRaidoHubDispatcher<DispatchHub>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IRaidoContext>(),
            provider.GetRequiredService<ILogger<DefaultRaidoHubDispatcher<DispatchHub>>>(),
            options);
    }

    [TestInitialize]
    public void ResetCounters()
    {
        DispatchHub.Connected = 0;
        DispatchHub.Disconnected = 0;
        DispatchHub.Invoked = 0;
        DispatchHub.FilterInvoked = 0;
        DispatchHub.AuthorizedInvoked = 0;
        DispatchHub.TaskInvoked = 0;
        DispatchHub.ValueTaskInvoked = 0;
        DispatchHub.ThrowFromHandler = false;
    }

    [TestMethod]
    public async Task Dispatcher_InvokesLifecycleAndMessageHandler()
    {
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();
        await dispatcher.OnConnectedAsync(connection);
        await dispatcher.DispatchMessageAsync(connection, new DispatchMessage());
        await dispatcher.OnDisconnectedAsync(connection, new Exception("closed"));
        Assert.AreEqual(1, DispatchHub.Connected);
        Assert.AreEqual(1, DispatchHub.Invoked);
        Assert.AreEqual(1, DispatchHub.Disconnected);
    }

    [TestMethod]
    public async Task Dispatcher_IgnoresUnknownMessagesAndRejectsUnauthorizedUserlessCalls()
    {
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();
        await dispatcher.DispatchMessageAsync(connection, new DispatchMessage());
        Assert.AreEqual(1, DispatchHub.Invoked);
        await dispatcher.DispatchMessageAsync(connection, new OtherMessage());
        Assert.AreEqual(0, DispatchHub.AuthorizedInvoked);
    }

    [TestMethod]
    public async Task Dispatcher_RunsFiltersAroundHubInvocation()
    {
        using var provider = CreateProvider(withFilter: true).Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();
        await dispatcher.OnConnectedAsync(connection);
        await dispatcher.DispatchMessageAsync(connection, new DispatchMessage());
        await dispatcher.OnDisconnectedAsync(connection, null);
        Assert.AreEqual(1, DispatchHub.FilterInvoked);
        Assert.AreEqual(1, DispatchHub.Invoked);
        Assert.AreEqual(1, DispatchHub.Connected);
        Assert.AreEqual(1, DispatchHub.Disconnected);
    }

    [TestMethod]
    public async Task Dispatcher_ExecutesTaskAndValueTaskHandlers()
    {
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();

        await dispatcher.DispatchMessageAsync(connection, new TaskMessage());
        await dispatcher.DispatchMessageAsync(connection, new ValueTaskMessage());

        Assert.AreEqual(1, DispatchHub.TaskInvoked);
        Assert.AreEqual(1, DispatchHub.ValueTaskInvoked);
    }

    [TestMethod]
    public async Task Dispatcher_StartsActivitiesWithLinkedOriginalActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RaidoServerActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();
        connection.OriginalActivity = new Activity("original").Start();

        await dispatcher.DispatchMessageAsync(connection, new DispatchMessage());

        connection.OriginalActivity.Stop();
        Assert.AreEqual(1, DispatchHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_RejectsAuthorizedCallWithoutUser()
    {
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        await dispatcher.DispatchMessageAsync(CreateConnection(), new OtherMessage());
        Assert.AreEqual(0, DispatchHub.AuthorizedInvoked);
    }

    [TestMethod]
    public async Task Dispatcher_InvokesAuthorizedCallForAuthenticatedUser()
    {
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();
        connection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity("test"))
        });

        await dispatcher.DispatchMessageAsync(connection, new OtherMessage());

        Assert.AreEqual(1, DispatchHub.AuthorizedInvoked);
    }

    [TestMethod]
    public async Task Dispatcher_SwallowsHandlerFailureAfterLoggingAndReleasesScope()
    {
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        DispatchHub.ThrowFromHandler = true;

        await dispatcher.DispatchMessageAsync(CreateConnection(), new DispatchMessage());

        Assert.AreEqual(1, DispatchHub.Invoked);
    }

    [TestMethod]
    public void Dispatcher_RejectsInvalidHubDefinitions()
    {
        using var provider = CreateProvider().Provider;
        Assert.ThrowsExactly<NotSupportedException>(() => new DefaultRaidoHubDispatcher<GenericHub>(
            provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IRaidoContext>(),
            NullLogger<DefaultRaidoHubDispatcher<GenericHub>>.Instance, Options.Create(new RaidoHubOptions<GenericHub>())));
        Assert.ThrowsExactly<NotSupportedException>(() => new DefaultRaidoHubDispatcher<InvalidMessageHub>(
            provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IRaidoContext>(),
            NullLogger<DefaultRaidoHubDispatcher<InvalidMessageHub>>.Instance, Options.Create(new RaidoHubOptions<InvalidMessageHub>())));
    }

    private sealed class GenericHub : RaidoHub
    {
        [RaidoMessageHandler(typeof(DispatchMessage))]
        public void Handle<T>(DispatchMessage message) { }
    }

    private sealed class InvalidMessageHub : RaidoHub
    {
        [RaidoMessageHandler(typeof(string))]
        public void Handle(DispatchMessage message) { }
    }

    private sealed class UserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }
}
