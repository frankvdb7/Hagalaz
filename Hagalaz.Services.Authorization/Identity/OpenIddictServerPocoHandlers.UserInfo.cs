using System.Collections.Immutable;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Hagalaz.Services.Authorization.Identity
{
    public static partial class OpenIddictServerPocoHandlers
    {
        public static class UserInfo
        {
            public static ImmutableArray<OpenIddictServerHandlerDescriptor> DefaultHandlers { get; } = ImmutableArray.Create(
                ProcessPocoResponse<ApplyUserInfoResponseContext>.Descriptor
            );
        }
    }
}
