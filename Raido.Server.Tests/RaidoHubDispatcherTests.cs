using System.Buffers;
using System.Collections.Concurrent;
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
    private readonly List<RaidoHubConnectionContext> _connections = new();
    private readonly List<(Pipe Input, Pipe Output)> _transports = new();

    private sealed class DispatchMessage : RaidoMessage { }
    private sealed class OtherMessage : RaidoMessage { }
    private sealed class TaskMessage : RaidoMessage { }
    private sealed class ValueTaskMessage : RaidoMessage { }
    private sealed class ClassAuthorizedMessage : RaidoMessage { }
    private sealed class SecondClassAuthorizedMessage : RaidoMessage { }
    private sealed class RoleProtectedMessage : RaidoMessage { }
    private sealed class MethodRoleProtectedMessage : RaidoMessage { }
    private sealed class CombinedAuthorizationMessage : RaidoMessage { }
    private sealed class InheritedAuthorizationMessage : RaidoMessage { }
    private sealed class MultiplePolicyMessage : RaidoMessage { }
    private sealed class AllowAnonymousMessage : RaidoMessage { }
    private class BaseMessage : RaidoMessage { }
    private sealed class DerivedMessage : BaseMessage { }

    private sealed class ExactTypeDispatchTracker
    {
        public int Invoked;
    }

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

    private sealed class ExactTypeDispatchHub : RaidoHub
    {
        [RaidoMessageHandler(typeof(BaseMessage))]
        public void Handle(BaseMessage message, ExactTypeDispatchTracker tracker) => tracker.Invoked++;
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

    [Authorize]
    private sealed class ClassAuthorizedHub : RaidoHub
    {
        public static int Invoked;

        [RaidoMessageHandler(typeof(ClassAuthorizedMessage))]
        public void Handle(ClassAuthorizedMessage message) => Invoked++;

        [RaidoMessageHandler(typeof(SecondClassAuthorizedMessage))]
        public void HandleSecond(SecondClassAuthorizedMessage message) => Invoked++;
    }

    [Authorize(Roles = "admin")]
    private sealed class RoleProtectedHub : RaidoHub
    {
        public static int Invoked;

        [RaidoMessageHandler(typeof(RoleProtectedMessage))]
        public void Handle(RoleProtectedMessage message) => Invoked++;
    }

    private sealed class MethodRoleProtectedHub : RaidoHub
    {
        public static int Invoked;

        [Authorize(Roles = "admin")]
        [RaidoMessageHandler(typeof(MethodRoleProtectedMessage))]
        public void Handle(MethodRoleProtectedMessage message) => Invoked++;
    }

    [Authorize]
    private sealed class CombinedAuthorizationHub : RaidoHub
    {
        public static int Invoked;

        [Authorize(Roles = "admin")]
        [RaidoMessageHandler(typeof(CombinedAuthorizationMessage))]
        public void Handle(CombinedAuthorizationMessage message) => Invoked++;
    }

    [Authorize]
    private abstract class BaseAuthorizedHub : RaidoHub
    {
        public static int Invoked;

        [RaidoMessageHandler(typeof(InheritedAuthorizationMessage))]
        public void Handle(InheritedAuthorizationMessage message) => Invoked++;
    }

    private sealed class InheritedAuthorizationHub : BaseAuthorizedHub
    {
    }

    [AllowAnonymous]
    private sealed class ClassAllowAnonymousOverrideHub : BaseAuthorizedHub
    {
    }

    [Authorize(Roles = "admin")]
    [Authorize(Policy = "trusted")]
    private sealed class MultiplePolicyHub : RaidoHub
    {
        public static int Invoked;

        [RaidoMessageHandler(typeof(MultiplePolicyMessage))]
        public void Handle(MultiplePolicyMessage message) => Invoked++;
    }

    [Authorize]
    private sealed class AllowAnonymousOverrideHub : RaidoHub
    {
        public static int Invoked;

        [AllowAnonymous]
        [RaidoMessageHandler(typeof(AllowAnonymousMessage))]
        public void Handle(AllowAnonymousMessage message) => Invoked++;
    }

    private static (ServiceProvider Provider, IRaidoContext Context) CreateProvider(bool withFilter = false)
        => CreateProvider<DispatchHub>(withFilter);

    private static (ServiceProvider Provider, IRaidoContext Context) CreateProvider<THub>(
        bool withFilter = false,
        Action<AuthorizationOptions>? configureAuthorization = null) where THub : RaidoHub
    {
        var services = new ServiceCollection();
        var context = Substitute.For<IRaidoContext>();
        context.Clients.Returns(Substitute.For<IRaidoClients>());
        services.AddSingleton(context);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        services.AddAuthorization(configureAuthorization ?? (_ => { }));
        services.AddSingleton(new RaidoServerActivitySource());
        services.AddSingleton<ExactTypeDispatchTracker>();
        services.AddScoped<IRaidoCallerContextAccessor, DefaultRaidoCallerContextAccessor>();
        services.AddScoped<IRaidoHubActivator<THub>, DefaultRaidoHubActivator<THub>>();
        services.AddOptions<RaidoHubOptions<THub>>();
        if (withFilter)
        {
            services.Configure<RaidoHubOptions<THub>>(options => options.AddFilter(new DispatchFilter()));
        }
        return (services.BuildServiceProvider(), context);
    }

    [TestCleanup]
    public void CleanupConnections()
    {
        foreach (var connection in _connections)
        {
            connection.Abort();
            connection.Cleanup();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }

    private RaidoHubConnectionContext CreateConnection(string id = "connection")
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
        _transports.Add((input, output));
        var connection = new RaidoHubConnectionContext(context, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance)
        {
            Protocol = new TestProtocol()
        };
        _connections.Add(connection);
        return connection;
    }

    private static DefaultRaidoHubDispatcher<DispatchHub> CreateDispatcher(ServiceProvider provider)
        => CreateDispatcher<DispatchHub>(provider);

    private static DefaultRaidoHubDispatcher<THub> CreateDispatcher<THub>(ServiceProvider provider) where THub : RaidoHub
    {
        var options = provider.GetRequiredService<IOptions<RaidoHubOptions<THub>>>();
        return new DefaultRaidoHubDispatcher<THub>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IRaidoContext>(),
            provider.GetRequiredService<ILogger<DefaultRaidoHubDispatcher<THub>>>(),
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
        ClassAuthorizedHub.Invoked = 0;
        RoleProtectedHub.Invoked = 0;
        MethodRoleProtectedHub.Invoked = 0;
        CombinedAuthorizationHub.Invoked = 0;
        BaseAuthorizedHub.Invoked = 0;
        MultiplePolicyHub.Invoked = 0;
        AllowAnonymousOverrideHub.Invoked = 0;
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
    public async Task Dispatcher_DoesNotDispatchDerivedMessageToBaseMessageHandler()
    {
        using var provider = CreateProvider<ExactTypeDispatchHub>().Provider;
        var dispatcher = CreateDispatcher<ExactTypeDispatchHub>(provider);
        var tracker = provider.GetRequiredService<ExactTypeDispatchTracker>();

        await dispatcher.DispatchMessageAsync(CreateConnection(), new DerivedMessage());

        Assert.AreEqual(0, tracker.Invoked);
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
        ActivitySource.AddActivityListener(listener);
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();
        connection.OriginalActivity = new Activity("original").Start();

        await dispatcher.DispatchMessageAsync(connection, new DispatchMessage());

        connection.OriginalActivity.Stop();
        Assert.AreEqual(1, DispatchHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_CreatesActivitiesForLifecycleCallbacks()
    {
        var started = new ConcurrentBag<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => started.Add(activity.OperationName)
        };
        ActivitySource.AddActivityListener(listener);
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        var connection = CreateConnection();

        await dispatcher.OnConnectedAsync(connection);
        await dispatcher.OnDisconnectedAsync(connection, null);

        Assert.Contains(RaidoServerActivitySource.OnConnected, started);
        Assert.Contains(RaidoServerActivitySource.OnDisconnected, started);
    }

    [TestMethod]
    public async Task Dispatcher_MarksHandlerFailureActivityAsError()
    {
        var stopped = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => stopped.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        using var provider = CreateProvider().Provider;
        var dispatcher = CreateDispatcher(provider);
        DispatchHub.ThrowFromHandler = true;

        await dispatcher.DispatchMessageAsync(CreateConnection(), new DispatchMessage());

        var activity = stopped.Single(candidate => candidate.OperationName == RaidoServerActivitySource.DispatchMessage);
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, activity.GetTagItem("error.type"));
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
    public async Task Dispatcher_EnforcesClassLevelAuthorizeAttributeForAllHandlers()
    {
        using var provider = CreateProvider<ClassAuthorizedHub>().Provider;
        var dispatcher = CreateDispatcher<ClassAuthorizedHub>(provider);

        var anonymousConnection = CreateConnection();
        await dispatcher.DispatchMessageAsync(anonymousConnection, new ClassAuthorizedMessage());
        await dispatcher.DispatchMessageAsync(anonymousConnection, new SecondClassAuthorizedMessage());

        Assert.AreEqual(0, ClassAuthorizedHub.Invoked);

        var authenticatedConnection = CreateConnection();
        authenticatedConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity("test"))
        });

        await dispatcher.DispatchMessageAsync(authenticatedConnection, new ClassAuthorizedMessage());
        await dispatcher.DispatchMessageAsync(authenticatedConnection, new SecondClassAuthorizedMessage());

        Assert.AreEqual(2, ClassAuthorizedHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_EnforcesClassLevelRoleAuthorizeAttribute()
    {
        using var provider = CreateProvider<RoleProtectedHub>().Provider;
        var dispatcher = CreateDispatcher<RoleProtectedHub>(provider);

        var nonAdminConnection = CreateConnection();
        nonAdminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, "user") }, "test"))
        });

        await dispatcher.DispatchMessageAsync(nonAdminConnection, new RoleProtectedMessage());

        Assert.AreEqual(0, RoleProtectedHub.Invoked);

        var adminConnection = CreateConnection();
        adminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, "admin") }, "test"))
        });

        await dispatcher.DispatchMessageAsync(adminConnection, new RoleProtectedMessage());

        Assert.AreEqual(1, RoleProtectedHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_EnforcesMethodLevelRoleAuthorizeAttribute()
    {
        using var provider = CreateProvider<MethodRoleProtectedHub>().Provider;
        var dispatcher = CreateDispatcher<MethodRoleProtectedHub>(provider);

        var nonAdminConnection = CreateConnection();
        nonAdminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, "user") }, "test"))
        });

        await dispatcher.DispatchMessageAsync(nonAdminConnection, new MethodRoleProtectedMessage());

        Assert.AreEqual(0, MethodRoleProtectedHub.Invoked);

        var adminConnection = CreateConnection();
        adminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, "admin") }, "test"))
        });

        await dispatcher.DispatchMessageAsync(adminConnection, new MethodRoleProtectedMessage());

        Assert.AreEqual(1, MethodRoleProtectedHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_CombinesClassAndMethodAuthorizeAttributes()
    {
        using var provider = CreateProvider<CombinedAuthorizationHub>().Provider;
        var dispatcher = CreateDispatcher<CombinedAuthorizationHub>(provider);

        await dispatcher.DispatchMessageAsync(CreateConnection(), new CombinedAuthorizationMessage());

        var nonAdminConnection = CreateConnection();
        nonAdminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, "user") }, "test"))
        });

        await dispatcher.DispatchMessageAsync(nonAdminConnection, new CombinedAuthorizationMessage());

        Assert.AreEqual(0, CombinedAuthorizationHub.Invoked);

        var adminConnection = CreateConnection();
        adminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, "admin") }, "test"))
        });

        await dispatcher.DispatchMessageAsync(adminConnection, new CombinedAuthorizationMessage());

        Assert.AreEqual(1, CombinedAuthorizationHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_EnforcesInheritedClassLevelAuthorizeAttribute()
    {
        using var provider = CreateProvider<InheritedAuthorizationHub>().Provider;
        var dispatcher = CreateDispatcher<InheritedAuthorizationHub>(provider);

        await dispatcher.DispatchMessageAsync(CreateConnection(), new InheritedAuthorizationMessage());

        Assert.AreEqual(0, BaseAuthorizedHub.Invoked);

        var authenticatedConnection = CreateConnection();
        authenticatedConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity("test"))
        });

        await dispatcher.DispatchMessageAsync(authenticatedConnection, new InheritedAuthorizationMessage());

        Assert.AreEqual(1, BaseAuthorizedHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_AllowsClassLevelAllowAnonymousToOverrideInheritedAuthorization()
    {
        using var provider = CreateProvider<ClassAllowAnonymousOverrideHub>().Provider;
        var dispatcher = CreateDispatcher<ClassAllowAnonymousOverrideHub>(provider);

        await dispatcher.DispatchMessageAsync(CreateConnection(), new InheritedAuthorizationMessage());

        Assert.AreEqual(1, BaseAuthorizedHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_CombinesMultipleClassLevelAuthorizePolicies()
    {
        using var provider = CreateProvider<MultiplePolicyHub>(configureAuthorization: options =>
            options.AddPolicy("trusted", policy => policy.RequireClaim("trusted", "yes"))).Provider;
        var dispatcher = CreateDispatcher<MultiplePolicyHub>(provider);

        var trustedNonAdminConnection = CreateConnection();
        trustedNonAdminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Role, "user"),
                    new Claim("trusted", "yes")
                }, "test"))
        });

        await dispatcher.DispatchMessageAsync(trustedNonAdminConnection, new MultiplePolicyMessage());

        var untrustedAdminConnection = CreateConnection();
        untrustedAdminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, "admin") }, "test"))
        });

        await dispatcher.DispatchMessageAsync(untrustedAdminConnection, new MultiplePolicyMessage());

        Assert.AreEqual(0, MultiplePolicyHub.Invoked);

        var trustedAdminConnection = CreateConnection();
        trustedAdminConnection.Features.Set<IConnectionUserFeature>(new UserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Role, "admin"),
                    new Claim("trusted", "yes")
                }, "test"))
        });

        await dispatcher.DispatchMessageAsync(trustedAdminConnection, new MultiplePolicyMessage());

        Assert.AreEqual(1, MultiplePolicyHub.Invoked);
    }

    [TestMethod]
    public async Task Dispatcher_AllowsMethodLevelAllowAnonymousToOverrideHubAuthorization()
    {
        using var provider = CreateProvider<AllowAnonymousOverrideHub>().Provider;
        var dispatcher = CreateDispatcher<AllowAnonymousOverrideHub>(provider);

        await dispatcher.DispatchMessageAsync(CreateConnection(), new AllowAnonymousMessage());

        Assert.AreEqual(1, AllowAnonymousOverrideHub.Invoked);
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
