using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using Hagalaz.Services.Authorization.Services;
using MassTransit;
using MassTransit.Mediator;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;


namespace Hagalaz.Services.Authorization.Consumers
{
    public class SignInUserRequestConsumer : IConsumer<SignInUserRequestMessage>
    {
        private readonly IOpenIddictService _openIddictService;
        private readonly IRequestClient<PasswordGrantCommand> _requestClientPasswordGrantCommand;
        private readonly IRequestClient<GetTokensRequestMessage> _requestClientGetTokensRequestCommand;

        public SignInUserRequestConsumer(IMediator mediator, IOpenIddictService openIddictService)
        {
            _requestClientPasswordGrantCommand = mediator.CreateRequestClient<PasswordGrantCommand>();
            _requestClientGetTokensRequestCommand = mediator.CreateRequestClient<GetTokensRequestMessage>();
            _openIddictService = openIddictService;
        }

        public async Task Consume(ConsumeContext<SignInUserRequestMessage> context)
        {
            var message = context.Message;
            var signInResponse = await _requestClientPasswordGrantCommand.GetResponse<PasswordGrantResult>(new PasswordGrantCommand(message.Login, message.Password, message.Scopes));
            var signInResult = signInResponse.Message;
            if (!signInResult.Succeeded)
            {
                await context.RespondAsync(new SignInUserResponseMessage()
                {
                    IsLockedOut = signInResult.IsLockedOut, AreCredentialsInvalid = signInResult.AreCredentialsInvalid, IsDisabled = signInResult.IsDisabled
                });
                return;
            }
            try
            {
                var subject = signInResult.User.GetClaim(Claims.Subject);
                if (subject != null)
                {
                    foreach (var clientScope in message.ClientScopes)
                    {
                        var tokenResponse = await _requestClientGetTokensRequestCommand.GetResponse<GetTokensResponseMessage>(new GetTokensRequestMessage(clientScope, subject)
                        {
                            Status = Statuses.Valid
                        });
                        var tokenResult = tokenResponse.Message;
                        if (tokenResult.Tokens.Any())
                        {
                            await context.RespondAsync(new SignInUserResponseMessage
                            {
                                IsAuthenticated = true
                            });
                            return;
                        }
                    }
                }
            } 
            catch(Exception ex)
            {
                await context.RespondAsync(new SignInUserResponseMessage
                {
                    Error = ex.Message
                });
                return;
            }

            // openiddict token creation
            var transaction = await _openIddictService.CreateTransactionAsync();
            var response = new OpenIddictResponse();
            var processSignIn = new ProcessSignInContext(transaction)
            {
                Principal = signInResult.User,
                EndpointType = OpenIddictServerEndpointType.Token,
                Request = new OpenIddictRequest()
                {
                    ClientId = message.ClientId
                },
                Response = response
            };
            await _openIddictService.DispatchAsync(processSignIn);

            if (response.IdToken == null || response.AccessToken == null || processSignIn.IsRequestSkipped || processSignIn.IsRejected)
            {
                await context.RespondAsync(new SignInUserResponseMessage
                {
                    Error = processSignIn.Error
                });
                return;
            }

            await context.RespondAsync(new SignInUserResponseMessage()
            {
                Succeeded = true,
                IdToken = response.IdToken,
                AccessToken = response.AccessToken,
                Scope = response.Scope,
                ExpireDate = response.ExpiresIn.HasValue ? 
                            DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresIn.Value) :
                            DateTimeOffset.UtcNow.Add(TimeSpan.FromHours(1)),
                TokenType = response.TokenType
            });
        }
    }
}