using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using static Hagalaz.Services.Authorization.Identity.OpenIddictServerPocoEvents;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.Authorization.Identity
{
    public static partial class OpenIddictServerPocoHandlers
    {
        public static class Revocation
        {
            public static ImmutableArray<OpenIddictServerHandlerDescriptor> DefaultHandlers { get; } = ImmutableArray.Create(
                // process
                HandlePocoRevocationRequest.Descriptor,

                // response
                ProcessPocoResponse<HandleRevocationRequestContext>.Descriptor,
                ProcessPocoResponse<ApplyRevocationResponseContext>.Descriptor
            );

            public class HandlePocoRevocationRequest : IOpenIddictServerHandler<ValidateRevocationRequestContext> 
            {
                private readonly IOpenIddictServerDispatcher _dispatcher;

                public HandlePocoRevocationRequest(IOpenIddictServerDispatcher dispatcher)
                    => _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

                public static OpenIddictServerHandlerDescriptor Descriptor { get; }
                    = OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateRevocationRequestContext>()
                    .AddFilter<OpenIddictServerPocoEvents.RequirePocoRequest>()
                    .UseScopedHandler<HandlePocoRevocationRequest>()
                    .SetOrder(int.MaxValue - 100)
                    .SetType(OpenIddictServerHandlerType.Custom)
                    .Build();

                public async ValueTask HandleAsync(ValidateRevocationRequestContext context)
                {
                    if (context is null)
                    {
                        throw new ArgumentNullException(nameof(context));
                    }
                    // Store the context object in the transaction so it can be later retrieved by handlers
                    // that want to access the principal without triggering a new validation process.
                    context.Transaction.SetProperty(context.GetType().FullName!, context);

                    var notification = new HandleRevocationRequestContext(context.Transaction);
                    await _dispatcher.DispatchAsync(notification);

                    if (notification.IsRequestHandled)
                    {
                        context.HandleRequest();
                        return;
                    }
                    else if (notification.IsRequestSkipped)
                    {
                        notification.SkipRequest();
                        return;
                    } 
                    else if (notification.IsRejected)
                    {
                        context.Reject(
                            error: notification.Error ?? Errors.InvalidRequest,
                            description: notification.ErrorDescription,
                            uri: notification.ErrorUri);
                        return;
                    }
                }
            }
        }
    }
}
