using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using Hagalaz.Services.Authorization.Services;
using MassTransit;
using MassTransit.Mediator;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;
using Microsoft.Extensions.Logging;
using static OpenIddict.Server.OpenIddictServerEvents;


namespace Hagalaz.Services.Authorization.Consumers
{
    public class SignInUserRequestConsumer : IConsumer<SignInUserRequestMessage>
    {
        private readonly IOpenIddictService _openIddictService;
        private readonly OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> _applicationManager;
        private readonly OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization> _authorizationManager;
        private readonly OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> _tokenManager;
        private readonly IRequestClient<PasswordGrantCommand> _requestClientPasswordGrantCommand;
        private readonly ILogger<SignInUserRequestConsumer> _logger;

        public SignInUserRequestConsumer(
            IMediator mediator,
            IOpenIddictService openIddictService,
            OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> applicationManager,
            OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization> authorizationManager,
            OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager,
            ILogger<SignInUserRequestConsumer> logger)
        {
            _requestClientPasswordGrantCommand = mediator.CreateRequestClient<PasswordGrantCommand>();
            _openIddictService = openIddictService;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _tokenManager = tokenManager;
            _logger = logger;
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
            var application = await _applicationManager.FindByClientIdAsync(message.ClientId, context.CancellationToken);
            var applicationId = application == null
                ? null
                : await _applicationManager.GetIdAsync(application, context.CancellationToken);
            var subject = signInResult.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            if (string.IsNullOrWhiteSpace(applicationId) || string.IsNullOrWhiteSpace(subject))
            {
                await context.RespondAsync(new SignInUserResponseMessage
                {
                    Error = OpenIddictResources.FormatID6161(message.ClientId)
                });
                return;
            }

            OpenIddictEntityFrameworkCoreAuthorization? authorization = null;
            string? authorizationId = null;
            var authorizationCommitted = false;
            Exception? primaryFailure = null;
            try
            {
                authorization = await _authorizationManager.CreateAsync(
                    signInResult.User,
                    subject,
                    applicationId,
                    OpenIddictConstants.AuthorizationTypes.AdHoc,
                    message.Scopes,
                    context.CancellationToken);
                authorizationId = await _authorizationManager.GetIdAsync(authorization, context.CancellationToken);
                if (string.IsNullOrWhiteSpace(authorizationId))
                {
                    await context.RespondAsync(new SignInUserResponseMessage
                    {
                        Error = OpenIddictResources.FormatID6160(message.ClientId)
                    });
                    return;
                }

                signInResult.User.SetAuthorizationId(authorizationId);

                // openiddict token creation
                var transaction = await _openIddictService.CreateTransactionAsync();
                var response = new OpenIddictResponse();
                var processSignIn = new ProcessSignInContext(transaction)
                {
                    Principal = signInResult.User,
                    EndpointType = OpenIddictServerEndpointType.Token,
                    Request = new OpenIddictRequest
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

                await context.RespondAsync(new SignInUserResponseMessage
                {
                    Succeeded = true,
                    IdToken = response.IdToken,
                    AccessToken = response.AccessToken,
                    Scope = response.Scope,
                    Subject = subject,
                    AuthorizationId = authorizationId,
                    ExpireDate = response.ExpiresIn.HasValue ?
                                DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresIn.Value) :
                                DateTimeOffset.UtcNow.Add(TimeSpan.FromHours(1)),
                    TokenType = response.TokenType
                });
                authorizationCommitted = true;
            }
            catch (Exception exception)
            {
                primaryFailure = exception;
                throw;
            }
            finally
            {
                if (authorization is not null && !authorizationCommitted)
                {
                    var cleanupFailure = await CleanupAuthorizationAsync(authorization, authorizationId);
                    if (cleanupFailure is not null)
                    {
                        if (primaryFailure is not null)
                        {
                            throw new AggregateException("Authorization issuance and cleanup both failed.", primaryFailure, cleanupFailure);
                        }

                        throw cleanupFailure;
                    }
                }
            }
        }

        private async Task<Exception?> CleanupAuthorizationAsync(
            OpenIddictEntityFrameworkCoreAuthorization authorization,
            string? authorizationId)
        {
            List<Exception>? failures = null;

            if (!string.IsNullOrWhiteSpace(authorizationId))
            {
                try
                {
                    await _tokenManager.RevokeByAuthorizationIdAsync(authorizationId, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    (failures ??= []).Add(exception);
                }
            }

            try
            {
                await _authorizationManager.DeleteAsync(authorization, CancellationToken.None);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }

            if (failures is null)
            {
                return null;
            }

            var cleanupFailure = new AggregateException("Failed to clean up the uncommitted authorization.", failures);
            _logger.LogError(cleanupFailure, "Failed to clean up authorization '{AuthorizationId}' after sign-in failed.", authorizationId);
            return cleanupFailure;
        }
    }
}
