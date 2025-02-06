using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Internal.Diagnostics;
using Raido.Server.Internal.Reflection;

namespace Raido.Server.Internal
{
    internal static class TaskCache
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
    }

    internal class DefaultRaidoHubDispatcher<THub> : IRaidoHubDispatcher where THub : RaidoHub
    {
        private static readonly string _fullHubName = typeof(THub).FullName ?? typeof(THub).Name;

        private readonly Dictionary<Type, RaidoHubMethodDescriptor> _messageHandlers = new();
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IRaidoContext _raidoContext;
        private readonly ILogger<DefaultRaidoHubDispatcher<THub>> _logger;

        private readonly Func<RaidoHubInvocationContext, ValueTask<object?>>? _invokeMiddleware;
        private readonly Func<RaidoHubLifetimeContext, Task>? _onConnectedMiddleware;
        private readonly Func<RaidoHubLifetimeContext, Exception?, Task>? _onDisconnectedMiddleware;

        public DefaultRaidoHubDispatcher(
            IServiceScopeFactory serviceScopeFactory,
            IRaidoContext raidoContext,
            ILogger<DefaultRaidoHubDispatcher<THub>> logger,
            IOptions<RaidoHubOptions<THub>> raidoOptions)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _raidoContext = raidoContext;
            _logger = logger;
            DiscoverHubMethods();

            var raidoHubFilters = raidoOptions.Value.HubFilters;
            var count = raidoHubFilters?.Count ?? 0;
            if (count != 0)
            {
                _invokeMiddleware = invocationContext =>
                {
                    var arguments = invocationContext.HubMethodArguments as object?[] ?? invocationContext.HubMethodArguments.ToArray();
                    return DefaultRaidoHubDispatcher<THub>.ExecuteMethod(invocationContext.ObjectMethodExecutor, invocationContext.Hub, arguments);
                };

                _onConnectedMiddleware = (context) => context.Hub.OnConnectedAsync();
                _onDisconnectedMiddleware = (context, exception) => context.Hub.OnDisconnectedAsync(exception);

                for (var i = count - 1; i > -1; i--)
                {
                    var resolvedFilter = raidoHubFilters![i];
                    var nextFilter = _invokeMiddleware;
                    _invokeMiddleware = (context) => resolvedFilter.InvokeMethodAsync(context, nextFilter);

                    var connectedFilter = _onConnectedMiddleware;
                    _onConnectedMiddleware = (context) => resolvedFilter.OnConnectedAsync(context, connectedFilter);

                    var disconnectedFilter = _onDisconnectedMiddleware;
                    _onDisconnectedMiddleware = (context, exception) => resolvedFilter.OnDisconnectedAsync(context, exception, disconnectedFilter);
                }
            }
        }

        public async Task OnConnectedAsync(RaidoConnectionContext connection)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            connection.RaidoCallerClients = new DefaultRaidoCallerClients(_raidoContext.Clients, connection.ConnectionId);

            var contextAccessor = scope.ServiceProvider.GetRequiredService<IRaidoCallerContextAccessor>();
            var hubActivator = scope.ServiceProvider.GetRequiredService<IRaidoHubActivator<THub>>();
            var hub = hubActivator.Create();
            Activity? activity = null;
            try
            {
                InitializeHub(hub, connection);

                contextAccessor.Context = connection.RaidoCallerContext;

                activity = StartActivity(RaidoServerActivitySource.OnConnected,
                    ActivityKind.Internal,
                    linkedActivity: null,
                    scope.ServiceProvider,
                    nameof(hub.OnConnectedAsync),
                    headers: null,
                    _logger);

                if (_onConnectedMiddleware != null)
                {
                    var context = new RaidoHubLifetimeContext(connection.RaidoCallerContext, scope.ServiceProvider, hub);
                    await _onConnectedMiddleware(context);
                }
                else
                {
                    await hub.OnConnectedAsync();
                }
            }
            catch (Exception ex)
            {
                SetActivityError(activity, ex);
                throw;
            }
            finally
            {
                activity?.Stop();
                hubActivator.Release(hub);
            }
        }

        public async Task OnDisconnectedAsync(RaidoConnectionContext connection, Exception? exception)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            var contextAccessor = scope.ServiceProvider.GetRequiredService<IRaidoCallerContextAccessor>();
            var hubActivator = scope.ServiceProvider.GetRequiredService<IRaidoHubActivator<THub>>();
            var hub = hubActivator.Create();
            Activity? activity = null;
            try
            {
                InitializeHub(hub, connection);

                contextAccessor.Context = connection.RaidoCallerContext;

                activity = StartActivity(RaidoServerActivitySource.OnDisconnected,
                    ActivityKind.Internal,
                    linkedActivity: null,
                    scope.ServiceProvider,
                    nameof(hub.OnDisconnectedAsync),
                    headers: null,
                    _logger);

                if (_onDisconnectedMiddleware != null)
                {
                    var context = new RaidoHubLifetimeContext(connection.RaidoCallerContext, scope.ServiceProvider, hub);
                    await _onDisconnectedMiddleware(context, exception);
                }
                else
                {
                    await hub.OnDisconnectedAsync(exception);
                }
            }
            catch (Exception ex)
            {
                SetActivityError(activity, ex);
                throw;
            }
            finally
            {
                activity?.Stop();
                hubActivator.Release(hub);
            }
        }

        public async Task DispatchMessageAsync(RaidoConnectionContext connection, RaidoMessage message)
        {
            var messageType = message.GetType();
            if (!_messageHandlers.TryGetValue(messageType, out var descriptor))
            {
                return;
            }

            var methodExecutor = descriptor.MethodExecutor;

            var scope = _serviceScopeFactory.CreateAsyncScope();
            IRaidoHubActivator<THub>? hubActivator = null;
            THub? hub = null;
            var arguments = new object[descriptor.ParameterTypes.Count];
            var ctType = typeof(CancellationToken);
            Activity? activity = null;
            for (var parameterPointer = 0; parameterPointer < arguments.Length; parameterPointer++)
            {
                if (descriptor.ParameterTypes[parameterPointer] == messageType)
                {
                    arguments[parameterPointer] = message;
                }
                else if (descriptor.ParameterTypes[parameterPointer] == ctType)
                {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAbortedToken);
                    arguments[parameterPointer] = cts;
                }
                else if (descriptor.IsServiceArgument(parameterPointer))
                {
                    arguments[parameterPointer] = scope.ServiceProvider.GetRequiredService(descriptor.ParameterTypes[parameterPointer]);
                }
            }

            try
            {
                hubActivator = scope.ServiceProvider.GetRequiredService<IRaidoHubActivator<THub>>();
                hub = hubActivator.Create();

                if (!await IsHubMethodAuthorized(scope.ServiceProvider, connection, descriptor, arguments, hub))
                {
                    Log.HubMethodNotAuthorized(_logger, descriptor.MethodExecutor.MethodInfo.Name);
                    return;
                }

                var contextAccessor = scope.ServiceProvider.GetRequiredService<IRaidoCallerContextAccessor>();

                try
                {
                    InitializeHub(hub, connection);

                    contextAccessor.Context = connection.RaidoCallerContext;

                    activity = StartActivity(RaidoServerActivitySource.DispatchMessage,
                        ActivityKind.Server,
                        connection.OriginalActivity,
                        scope.ServiceProvider,
                        descriptor.MethodExecutor.MethodInfo.Name,
                        headers: null,
                        _logger);

                    var result = await ExecuteHubMethod(methodExecutor, hub, arguments, connection, scope.ServiceProvider);
                    if (result is RaidoMessage response)
                    {
                        await connection.WriteAsync(response);
                    }
                }
                catch (Exception ex)
                {
                    SetActivityError(activity, ex);
                    Log.FailedInvokingHubMethod(_logger, methodExecutor.MethodInfo.Name, ex);
                }
            }
            finally
            {
                activity?.Stop();

                if (hub != null)
                {
                    hubActivator?.Release(hub);
                }

                await scope.DisposeAsync();
            }
        }

        private ValueTask<object?> ExecuteHubMethod(
            ObjectMethodExecutor methodExecutor,
            THub hub,
            object?[] arguments,
            RaidoConnectionContext connection,
            IServiceProvider serviceProvider)
        {
            if (_invokeMiddleware != null)
            {
                var invocationContext = new RaidoHubInvocationContext(methodExecutor, connection.RaidoCallerContext, serviceProvider, hub, arguments);
                return _invokeMiddleware(invocationContext);
            }

            // If no Hub filters are registered
            return DefaultRaidoHubDispatcher<THub>.ExecuteMethod(methodExecutor, hub, arguments);
        }

        private static async ValueTask<object?> ExecuteMethod(ObjectMethodExecutor methodExecutor, RaidoHub hub, object?[] arguments)
        {
            if (methodExecutor.IsMethodAsync)
            {
                if (methodExecutor.MethodReturnType == typeof(Task))
                {
                    await (Task)methodExecutor.Execute(hub, arguments);
                    return null;
                }
                else
                {
                    return await methodExecutor.ExecuteAsync(hub, arguments);
                }
            }
            else
            {
                return methodExecutor.Execute(hub, arguments);
            }
        }

        private static Task<bool> IsHubMethodAuthorized(
            IServiceProvider provider,
            RaidoConnectionContext hubConnectionContext,
            RaidoHubMethodDescriptor descriptor,
            object?[] hubMethodArguments,
            RaidoHub hub)
        {
            // If there are no policies we don't need to run auth
            if (descriptor.Policies.Count == 0)
            {
                return TaskCache.True;
            }

            // If there a policies but no user, we automatically return false
            if (hubConnectionContext.User == null)
            {
                return TaskCache.False;
            }

            return IsHubMethodAuthorizedSlow(provider,
                hubConnectionContext.User,
                descriptor.Policies,
                new RaidoHubInvocationContext(descriptor.MethodExecutor, hubConnectionContext.RaidoCallerContext, provider, hub, hubMethodArguments));
        }

        private static async Task<bool> IsHubMethodAuthorizedSlow(
            IServiceProvider provider,
            ClaimsPrincipal principal,
            IList<IAuthorizeData> policies,
            RaidoHubInvocationContext resource)
        {
            var authService = provider.GetRequiredService<IAuthorizationService>();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            // AuthorizationPolicy.CombineAsync only returns null if there are no policies and we check that above
            Debug.Assert(authorizePolicy != null);

            var authorizationResult = await authService.AuthorizeAsync(principal, resource, authorizePolicy);
            // Only check authorization success, challenge or forbid wouldn't make sense from a hub method invocation
            return authorizationResult.Succeeded;
        }

        private void InitializeHub(RaidoHub controller, RaidoConnectionContext connection)
        {
            controller.Clients = connection.RaidoCallerClients;
            controller.Context = connection.RaidoCallerContext;
        }

        private void DiscoverHubMethods()
        {
            var baseMessageType = typeof(RaidoMessage);
            var hubType = typeof(THub);
            var hubTypeInfo = hubType.GetTypeInfo();
            var hubName = hubType.Name;

            using var scope = _serviceScopeFactory.CreateScope();
            var serviceProviderIsService = scope.ServiceProvider.GetService<IServiceProviderIsService>();

            foreach (var methodInfo in RaidoHubReflectionHelper.GetHubMethods(hubType))
            {
                if (methodInfo.IsGenericMethod)
                {
                    throw new NotSupportedException($"Method '{methodInfo.Name}' is a generic method which is not supported on a Hub.");
                }

                var messageType = methodInfo.GetCustomAttribute<RaidoMessageHandlerAttribute>()?.Message;

                if (messageType == null)
                {
                    continue;
                }

                if (!messageType.IsAssignableTo(baseMessageType))
                {
                    throw new NotSupportedException($"Type '{messageType}' does not inherit base type '{nameof(RaidoMessage)}'.");
                }

                if (methodInfo.GetParameters().All(info => info.ParameterType != messageType))
                {
                    throw new NotSupportedException($"Method '{methodInfo.Name}' does not contain a parameter with type '{messageType.Name}'.");
                }

                if (_messageHandlers.ContainsKey(messageType))
                {
                    throw new NotSupportedException($"Duplicate definitions for type '{messageType}'. Overloading is not supported.");
                }

                var executor = ObjectMethodExecutor.Create(methodInfo, hubTypeInfo);
                var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                _messageHandlers[messageType] = new RaidoHubMethodDescriptor(executor, serviceProviderIsService, authorizeAttributes);

                Log.HubMethodBound(_logger, hubName, methodInfo.Name);
            }
        }

        private static Activity? StartActivity(
            string operationName, ActivityKind kind, Activity? linkedActivity, IServiceProvider serviceProvider, string methodName,
            IDictionary<string, string>? headers, ILogger logger)
        {
            var activitySource = serviceProvider.GetService<RaidoServerActivitySource>()?.ActivitySource;
            if (activitySource is null)
            {
                return null;
            }

            var loggingEnabled = logger.IsEnabled(LogLevel.Critical);
            if (!activitySource.HasListeners() && !loggingEnabled)
            {
                return null;
            }

            IEnumerable<KeyValuePair<string, object?>> tags =
            [
                new("tcp.method", methodName),
                new("tcp.system", "raido"),
                new("tcp.service", _fullHubName),
                // See https://github.com/dotnet/aspnetcore/blob/027c60168383421750f01e427e4f749d0684bc02/src/Servers/Kestrel/Core/src/Internal/Infrastructure/KestrelMetrics.cs#L308
                // And https://github.com/dotnet/aspnetcore/issues/43786
                //new("server.address", ...),
            ];
            IEnumerable<ActivityLink>? links = (linkedActivity is not null) ? [new ActivityLink(linkedActivity.Context)] : null;

            Activity? activity;
            if (headers != null)
            {
                var propagator = serviceProvider.GetService<DistributedContextPropagator>() ?? DistributedContextPropagator.Current;

                activity = ActivityCreator.CreateFromRemote(activitySource,
                    propagator,
                    headers,
                    static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
                    {
                        fieldValues = default;
                        var headers = (IDictionary<string, string>)carrier!;
                        headers.TryGetValue(fieldName, out fieldValue);
                    },
                    operationName,
                    kind,
                    tags,
                    links,
                    loggingEnabled);
            }
            else
            {
                activity = activitySource.CreateActivity(operationName, kind, parentId: null, tags: tags, links: links);
            }

            if (activity is not null)
            {
                activity.DisplayName = $"{_fullHubName}/{methodName}";
                activity.Start();
            }

            return activity;
        }

        private static void SetActivityError(Activity? activity, Exception ex)
        {
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetStatus(ActivityStatusCode.Error);
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception?> _hubMethodNotAuthorized = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1, "HubMethodNotAuthorized"),
                "Failed to invoke '{HubMethod}' because user is unauthorized.");

            private static readonly Action<ILogger, string, Exception> _failedInvokingHubMethod =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "FailedInvokingHubMethod"), "Failed to invoke hub method '{HubMethod}'.");

            private static readonly Action<ILogger, string, string, Exception?> _hubMethodBound =
                LoggerMessage.Define<string, string>(LogLevel.Trace, new EventId(3, "HubMethodBound"), "'{HubName}' hub message type '{HubMethod}' is bound.");

            public static void HubMethodNotAuthorized(ILogger logger, string hubMethod) => _hubMethodNotAuthorized(logger, hubMethod, null);

            public static void FailedInvokingHubMethod(ILogger logger, string hubMethod, Exception exception) =>
                _failedInvokingHubMethod(logger, hubMethod, exception);

            public static void HubMethodBound(ILogger logger, string hubName, string hubMethod) => _hubMethodBound(logger, hubName, hubMethod, null);
        }
    }
}