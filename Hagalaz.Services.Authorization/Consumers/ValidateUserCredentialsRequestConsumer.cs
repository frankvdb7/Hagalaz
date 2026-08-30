using System.Collections.Immutable;
using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using MassTransit;
using MassTransit.Mediator;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.Authorization.Consumers;

public sealed class ValidateUserCredentialsRequestConsumer : IConsumer<ValidateUserCredentialsRequestMessage>
{
    private readonly IRequestClient<PasswordGrantCommand> _passwordGrantClient;

    public ValidateUserCredentialsRequestConsumer(IMediator mediator) =>
        _passwordGrantClient = mediator.CreateRequestClient<PasswordGrantCommand>();

    public async Task Consume(ConsumeContext<ValidateUserCredentialsRequestMessage> context)
    {
        var result = (await _passwordGrantClient.GetResponse<PasswordGrantResult>(
            new PasswordGrantCommand(context.Message.Login, context.Message.Password, ImmutableArray<string>.Empty),
            context.CancellationToken)).Message;

        if (!result.Succeeded)
        {
            await context.RespondAsync(new ValidateUserCredentialsResponseMessage
            {
                IsLockedOut = result.IsLockedOut,
                IsDisabled = result.IsDisabled,
                AreCredentialsInvalid = result.AreCredentialsInvalid,
                IsNotAllowed = result.IsNotAllowed
            });
            return;
        }

        var subject = result.User.GetClaim(Claims.Subject);
        if (string.IsNullOrWhiteSpace(subject))
        {
            await context.RespondAsync(new ValidateUserCredentialsResponseMessage());
            return;
        }

        await context.RespondAsync(new ValidateUserCredentialsResponseMessage
        {
            Succeeded = true,
            Subject = subject
        });
    }
}
