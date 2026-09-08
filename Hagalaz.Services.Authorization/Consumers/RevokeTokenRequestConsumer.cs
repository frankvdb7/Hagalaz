using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using MassTransit;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.Authorization.Consumers
{
    public class RevokeTokenRequestConsumer : IConsumer<RevokeTokenRequestMessage>
    {
        private readonly OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> _tokenManager;
        private readonly OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> _applicationManager;
        private readonly OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization> _authorizationManager;

        public RevokeTokenRequestConsumer(
            OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager,
            OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> applicationManager,
            OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization> authorizationManager)
        {
            _tokenManager = tokenManager;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
        }

        public async Task Consume(ConsumeContext<RevokeTokenRequestMessage> context)
        {
            var message = context.Message;
            var application = await _applicationManager.FindByClientIdAsync(message.ClientId, context.CancellationToken);
            if (application == null)
            {
                await context.RespondAsync(new RevokeTokenResponseMessage { Error = OpenIddictResources.FormatID6161(message.ClientId) });
                return;
            }
            var client = await _applicationManager.GetIdAsync(application, context.CancellationToken);
            if (client == null)
            {
                await context.RespondAsync(new RevokeTokenResponseMessage { Error = OpenIddictResources.FormatID6160(message.ClientId) });
                return;
            }
            var authorization = await _authorizationManager.FindByIdAsync(message.AuthorizationId, context.CancellationToken);
            if (authorization == null)
            {
                await context.RespondAsync(new RevokeTokenResponseMessage { Succeeded = true });
                return;
            }

            var authorizationSubject = await _authorizationManager.GetSubjectAsync(authorization, context.CancellationToken);
            var authorizationApplication = await _authorizationManager.GetApplicationIdAsync(authorization, context.CancellationToken);
            if (!string.Equals(authorizationSubject, message.Subject, System.StringComparison.Ordinal) ||
                !string.Equals(authorizationApplication, client, System.StringComparison.Ordinal))
            {
                await context.RespondAsync(new RevokeTokenResponseMessage { Succeeded = true });
                return;
            }

            await _tokenManager.RevokeByAuthorizationIdAsync(message.AuthorizationId, context.CancellationToken);
            if (await _authorizationManager.GetStatusAsync(authorization, context.CancellationToken) == Statuses.Valid &&
                !await _authorizationManager.TryRevokeAsync(authorization, context.CancellationToken) &&
                await _authorizationManager.GetStatusAsync(authorization, context.CancellationToken) == Statuses.Valid)
            {
                await context.RespondAsync(new RevokeTokenResponseMessage { Error = OpenIddictResources.ID2079 });
                return;
            }

            await context.RespondAsync(new RevokeTokenResponseMessage
            {
                Succeeded = true
            });
        }
    }
}
