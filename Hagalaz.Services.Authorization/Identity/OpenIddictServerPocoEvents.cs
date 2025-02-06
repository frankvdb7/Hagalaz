using System;
using System.Threading.Tasks;
using OpenIddict.Server;

namespace Hagalaz.Services.Authorization.Identity
{
    public static partial class OpenIddictServerPocoEvents
    {
        public class RequirePocoRequest : IOpenIddictServerHandlerFilter<OpenIddictServerEvents.BaseContext>
        {
            public const string PocoRequestKey = "PocoRequest";

            public ValueTask<bool> IsActiveAsync(OpenIddictServerEvents.BaseContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }
                return new ValueTask<bool>(context.Transaction.Properties.ContainsKey(PocoRequestKey));
            }
        }
    }
}