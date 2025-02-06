using System;
using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using Hagalaz.Services.Authorization.Services;
using MassTransit;
using MassTransit.Mediator;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Hagalaz.Services.Authorization.Consumers
{
    public class GetUserInfoRequestConsumer : IConsumer<GetUserInfoRequestMessage>
    {
        private readonly IMediator _mediator;
        private readonly IOpenIddictService _openIddictService;
        private readonly IRequestClient<GetUserInfoCommand> _requestClientGetUserInfoCommand;

        public GetUserInfoRequestConsumer(IMediator mediator, IOpenIddictService openIddictService)
        {
            _mediator = mediator;
            _requestClientGetUserInfoCommand = _mediator.CreateRequestClient<GetUserInfoCommand>();
            _openIddictService = openIddictService;
        }

        public async Task Consume(ConsumeContext<GetUserInfoRequestMessage> context)
        {
            var message = context.Message;
            var transaction = await _openIddictService.CreateTransactionAsync();
            var processAuthentication = new ProcessAuthenticationContext(transaction)
            {
                EndpointType = OpenIddictServerEndpointType.UserInfo,
                Request = new OpenIddictRequest()
                {
                    AccessToken = message.AccessToken
                }
            };
            await _openIddictService.DispatchAsync(processAuthentication);

            if (processAuthentication.AccessTokenPrincipal == null || processAuthentication.IsRejected || processAuthentication.IsRequestSkipped)
            {
                throw new Exception("Failed to process authentication", new Exception(processAuthentication.Error));
            }
            
            var response = await _requestClientGetUserInfoCommand.GetResponse<GetUserInfoResult>(new GetUserInfoCommand(processAuthentication.AccessTokenPrincipal));
            var result = response.Message;
            await context.RespondAsync(new GetUserInfoResponseMessage
            {
                Succeeded = result.Claims != null,
                Claims = result.Claims
            });
        }
    }
}