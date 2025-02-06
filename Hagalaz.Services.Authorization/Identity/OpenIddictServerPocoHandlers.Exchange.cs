using System.Collections.Immutable;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Hagalaz.Services.Authorization.Identity
{
    public static partial class OpenIddictServerPocoHandlers
    {
        public static class Exchange
        {
            public static ImmutableArray<OpenIddictServerHandlerDescriptor> DefaultHandlers { get; } = ImmutableArray.Create(
                ProcessPocoResponse<ApplyTokenResponseContext>.Descriptor
            );
        }
    }
}
