using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using MassTransit;
using MassTransit.Mediator;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.Authorization.Consumers;

public sealed class ValidateExistingAuthenticationRequestConsumer : IConsumer<ValidateExistingAuthenticationRequestMessage>
{
    private readonly IRequestClient<PasswordGrantCommand> _passwordGrant;
    private readonly IRequestClient<GetTokensRequestMessage> _tokens;

    public ValidateExistingAuthenticationRequestConsumer(IMediator mediator)
    {
        _passwordGrant = mediator.CreateRequestClient<PasswordGrantCommand>();
        _tokens = mediator.CreateRequestClient<GetTokensRequestMessage>();
    }

    public async Task Consume(ConsumeContext<ValidateExistingAuthenticationRequestMessage> context)
    {
        var message = context.Message;
        var passwordGrant = await _passwordGrant.GetResponse<PasswordGrantResult>(
            new PasswordGrantCommand(message.Login, message.Password, message.Scopes));
        var result = passwordGrant.Message;
        if (!result.Succeeded)
        {
            await context.RespondAsync(new ValidateExistingAuthenticationResponseMessage
            {
                AreCredentialsInvalid = result.AreCredentialsInvalid,
                IsDisabled = result.IsDisabled,
                IsLockedOut = result.IsLockedOut
            });
            return;
        }

        try
        {
            var subject = result.User.GetClaim(Claims.Subject);
            if (subject is null)
            {
                await context.RespondAsync(new ValidateExistingAuthenticationResponseMessage());
                return;
            }

            foreach (var clientScope in message.ClientScopes)
            {
                var tokenResponse = await _tokens.GetResponse<GetTokensResponseMessage>(
                    new GetTokensRequestMessage(clientScope, subject) { Status = Statuses.Valid });
                if (tokenResponse.Message.Tokens.Any())
                {
                    await context.RespondAsync(new ValidateExistingAuthenticationResponseMessage
                    {
                        Succeeded = true,
                        Subject = subject
                    });
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await context.RespondAsync(new ValidateExistingAuthenticationResponseMessage());
            return;
        }

        await context.RespondAsync(new ValidateExistingAuthenticationResponseMessage());
    }
}
