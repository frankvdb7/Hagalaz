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

        public RevokeTokenRequestConsumer(OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager, OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> applicationManager)
        {
            _tokenManager = tokenManager;
            _applicationManager = applicationManager;
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
            var revokeFailed = false;
            await foreach (var token in _tokenManager.FindAsync(
                               message.Subject,
                               client,
                               Statuses.Valid,
                               type: null,
                               context.CancellationToken))
            {
                if (token.CreationDate is { } creationDate &&
                    creationDate > message.TokenCreatedBefore)
                {
                    continue;
                }

                if (await _tokenManager.TryRevokeAsync(token, context.CancellationToken))
                {
                    continue;
                }

                // Another logout may have won the race between the valid-token query and
                // the update. Treat that completed state transition as success; only keep
                // the failure when the token is still valid after the failed attempt.
                if (await _tokenManager.GetStatusAsync(token, context.CancellationToken) == Statuses.Valid)
                {
                    revokeFailed = true;
                }
            }
            if (revokeFailed)
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
