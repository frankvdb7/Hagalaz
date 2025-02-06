using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Raido.Server.Internal
{
    internal class RaidoHubFilterFactory : IRaidoHubFilter
    {
        private readonly ObjectFactory _objectFactory;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private readonly Type _filterType;

        public RaidoHubFilterFactory(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type filterType)
        {
            _objectFactory = ActivatorUtilities.CreateFactory(filterType, Array.Empty<Type>());
            _filterType = filterType;
        }

        public async ValueTask<object?> InvokeMethodAsync(RaidoHubInvocationContext invocationContext, Func<RaidoHubInvocationContext, ValueTask<object?>> next)
        {
            var (filter, owned) = GetFilter(invocationContext.ServiceProvider);

            try
            {
                return await filter.InvokeMethodAsync(invocationContext, next);
            }
            finally
            {
                if (owned)
                {
                    await DisposeFilter(filter);
                }
            }
        }

        public async Task OnConnectedAsync(RaidoHubLifetimeContext context, Func<RaidoHubLifetimeContext, Task> next)
        {
            var (filter, owned) = GetFilter(context.ServiceProvider);

            try
            {
                await filter.OnConnectedAsync(context, next);
            }
            finally
            {
                if (owned)
                {
                    await DisposeFilter(filter);
                }
            }
        }

        public async Task OnDisconnectedAsync(RaidoHubLifetimeContext context, Exception? exception, Func<RaidoHubLifetimeContext, Exception?, Task> next)
        {
            var (filter, owned) = GetFilter(context.ServiceProvider);

            try
            {
                await filter.OnDisconnectedAsync(context, exception, next);
            }
            finally
            {
                if (owned)
                {
                    await DisposeFilter(filter);
                }
            }
        }

        private ValueTask DisposeFilter(IRaidoHubFilter filter)
        {
            if (filter is IAsyncDisposable asyncDispsable)
            {
                return asyncDispsable.DisposeAsync();
            }

            if (filter is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return default;
        }

        private (IRaidoHubFilter, bool) GetFilter(IServiceProvider serviceProvider)
        {
            var owned = false;
            var filter = (IRaidoHubFilter?)serviceProvider.GetService(_filterType);
            if (filter == null)
            {
                filter = (IRaidoHubFilter)_objectFactory.Invoke(serviceProvider, Array.Empty<object>());
                owned = true;
            }

            return (filter, owned);
        }
    }
}