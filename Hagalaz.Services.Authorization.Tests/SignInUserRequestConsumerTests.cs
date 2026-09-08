using System.Collections.Immutable;
using System.Security.Claims;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Consumers;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using Hagalaz.Services.Authorization.Services;
using MassTransit;
using MassTransit.Mediator;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Hagalaz.Services.Authorization.Tests;

[TestClass]
public sealed class SignInUserRequestConsumerTests
{
    [TestMethod]
    public async Task Consume_WhenCredentialsAreInvalid_ReturnsCredentialFailureWithoutCreatingTokens()
    {
        var passwordGrant = new Mock<IRequestClient<PasswordGrantCommand>>();
        passwordGrant
            .Setup(client => client.GetResponse<PasswordGrantResult>(
                It.IsAny<PasswordGrantCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<RequestTimeout>()))
            .ReturnsAsync(CreateResponse(PasswordGrantResult.CredentialsInvalid));
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default))
            .Returns(passwordGrant.Object);
        var openIddict = new Mock<IOpenIddictService>();
        var context = new Mock<ConsumeContext<SignInUserRequestMessage>>();
        context.SetupGet(value => value.Message).Returns(CreateRequest());
        SignInUserResponseMessage? response = null;
        context
            .Setup(value => value.RespondAsync(It.IsAny<SignInUserResponseMessage>()))
            .Callback<SignInUserResponseMessage>(message => response = message)
            .Returns(Task.CompletedTask);

        var consumer = new SignInUserRequestConsumer(mediator.Object, openIddict.Object);

        await consumer.Consume(context.Object);

        Assert.IsNotNull(response);
        Assert.IsFalse(response!.Succeeded);
        Assert.IsTrue(response.AreCredentialsInvalid);
        openIddict.Verify(service => service.CreateTransactionAsync(), Times.Never);
        openIddict.Verify(service => service.DispatchAsync(It.IsAny<ProcessSignInContext>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_WhenValidPersistedTokenExists_IssuesFreshTokensInsteadOfReturningAlreadyAuthenticated()
    {
        var passwordGrant = new Mock<IRequestClient<PasswordGrantCommand>>();
        passwordGrant
            .Setup(client => client.GetResponse<PasswordGrantResult>(
                It.IsAny<PasswordGrantCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<RequestTimeout>()))
            .ReturnsAsync(CreateResponse(new PasswordGrantResult(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, "42")])))));

        var tokens = new Mock<IRequestClient<GetTokensRequestMessage>>();
        tokens
            .Setup(client => client.GetResponse<GetTokensResponseMessage>(
                It.IsAny<GetTokensRequestMessage>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<RequestTimeout>()))
            .ReturnsAsync(CreateResponse(new GetTokensResponseMessage
            {
                Tokens = [new GetTokensResponseMessage.TokenDto(
                    "token-id", "42", "access_token", OpenIddictConstants.Statuses.Valid, DateTime.UtcNow, DateTime.UtcNow.AddHours(1))]
            }));

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default))
            .Returns(passwordGrant.Object);
        mediator
            .Setup(value => value.CreateRequestClient<GetTokensRequestMessage>(default))
            .Returns(tokens.Object);

        var openIddict = new Mock<IOpenIddictService>();
        openIddict
            .Setup(service => service.CreateTransactionAsync())
            .Returns(new ValueTask<OpenIddictServerTransaction>(new OpenIddictServerTransaction()));
        openIddict
            .Setup(service => service.DispatchAsync(It.IsAny<ProcessSignInContext>()))
            .Callback<ProcessSignInContext>(context =>
            {
                context.Response.IdToken = "new-id-token";
                context.Response.AccessToken = "new-access-token";
                context.Response.Scope = "openid";
                context.Response.TokenType = "Bearer";
            });

        var context = new Mock<ConsumeContext<SignInUserRequestMessage>>();
        context.SetupGet(value => value.Message).Returns(CreateRequest());
        SignInUserResponseMessage? response = null;
        context
            .Setup(value => value.RespondAsync(It.IsAny<SignInUserResponseMessage>()))
            .Callback<SignInUserResponseMessage>(message => response = message)
            .Returns(Task.CompletedTask);

        var consumer = new SignInUserRequestConsumer(mediator.Object, openIddict.Object);

        await consumer.Consume(context.Object);

        Assert.IsNotNull(response);
        Assert.IsTrue(response!.Succeeded);
        Assert.AreEqual("new-access-token", response.AccessToken);
        tokens.Verify(client => client.GetResponse<GetTokensResponseMessage>(
            It.IsAny<GetTokensRequestMessage>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<RequestTimeout>()), Times.Never);
        openIddict.Verify(service => service.DispatchAsync(It.IsAny<ProcessSignInContext>()), Times.Once);
    }

    private static Response<T> CreateResponse<T>(T message) where T : class
    {
        var response = new Mock<Response<T>>();
        response.SetupGet(value => value.Message).Returns(message);
        return response.Object;
    }

    private static SignInUserRequestMessage CreateRequest() => new(
        "login",
        "password",
        "203.0.113.7",
        "game-client",
        ImmutableArray.Create("openid"),
        ImmutableArray.Create("game-client"));
}
