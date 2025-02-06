using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using MassTransit;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

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
            await foreach (var token in _tokenManager.FindBySubjectAsync(message.Subject, context.CancellationToken))
            {
                if (token.Application?.Id != application.Id)
                {
                    continue;
                }
                if (!await _tokenManager.TryRevokeAsync(token, context.CancellationToken))
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
