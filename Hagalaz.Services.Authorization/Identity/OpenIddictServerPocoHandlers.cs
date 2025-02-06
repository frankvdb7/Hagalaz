using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using OpenIddict.Server;
using static Hagalaz.Services.Authorization.Identity.OpenIddictServerPocoEvents;

namespace Hagalaz.Services.Authorization.Identity
{
    public static partial class OpenIddictServerPocoHandlers
    {
        public static ImmutableArray<OpenIddictServerHandlerDescriptor> DefaultHandlers { get; } = ImmutableArray<OpenIddictServerHandlerDescriptor>.Empty
            .AddRange(Exchange.DefaultHandlers)
            .AddRange(Revocation.DefaultHandlers)
            .AddRange(UserInfo.DefaultHandlers);

        public class ProcessPocoResponse<TContext> : IOpenIddictServerHandler<TContext> where TContext : OpenIddictServerEvents.BaseRequestContext
        {
            public static OpenIddictServerHandlerDescriptor Descriptor { get; }
                = OpenIddictServerHandlerDescriptor.CreateBuilder<TContext>()
                .AddFilter<OpenIddictServerPocoEvents.RequirePocoRequest>()
                .UseSingletonHandler<ProcessPocoResponse<TContext>>()
                .SetOrder(int.MaxValue - 1_000)
                .SetType(OpenIddictServerHandlerType.Custom)
                .Build();

            public ValueTask HandleAsync(TContext context)
            {
                if (context is null)
                {
                    throw new ArgumentNullException(nameof(context));
                }
                context.HandleRequest();
                return ValueTask.CompletedTask;
            }
        }
    }
}
