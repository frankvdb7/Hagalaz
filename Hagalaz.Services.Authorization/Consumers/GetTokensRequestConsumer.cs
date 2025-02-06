using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using MassTransit;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace Hagalaz.Services.Authorization.Consumers
{
    public class GetTokensRequestConsumer : IConsumer<GetTokensRequestMessage>
    {
        private readonly OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> _tokenManager;
        private readonly OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> _applicationManager;

        public GetTokensRequestConsumer(
            OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager,
            OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> applicationManager)
        {
            _tokenManager = tokenManager;
            _applicationManager = applicationManager;
        }

        public async Task Consume(ConsumeContext<GetTokensRequestMessage> context)
        {
            var message = context.Message;
            var application = await _applicationManager.FindByClientIdAsync(message.ClientId, context.CancellationToken);
            if (application == null)
            {
                await context.RespondAsync(new GetTokensResponseMessage
                {
                    Tokens = new List<GetTokensResponseMessage.TokenDto>()
                });
                return;
            }

            var applicationId = await _applicationManager.GetIdAsync(application, context.CancellationToken);
            if (applicationId == null)
            {
                await context.RespondAsync(new GetTokensResponseMessage
                {
                    Tokens = new List<GetTokensResponseMessage.TokenDto>()
                });
                return;
            }

            var tokens = _tokenManager.FindAsync(message.Subject, applicationId, message.Status, type: null, context.CancellationToken);

            await context.RespondAsync(new GetTokensResponseMessage
            {
                Tokens = await tokens.Select(token =>
                        new GetTokensResponseMessage.TokenDto(token.Id!, token.Subject!, token.Type!, token.Status!, token.CreationDate, token.ExpirationDate))
                    .ToListAsync()
            });
        }
    }
}