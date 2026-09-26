using System.Collections.Immutable;
using System.Security.Claims;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Consumers;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using MassTransit;
using MassTransit.Mediator;
using Moq;
using OpenIddict.Abstractions;

namespace Hagalaz.Services.Authorization.Tests;

[TestClass]
public sealed class ValidateExistingAuthenticationRequestConsumerTests
{
    [TestMethod]
    public async Task Consume_WhenExistingTokenLookupIsCanceled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
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
            .ThrowsAsync(new OperationCanceledException());
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default)).Returns(passwordGrant.Object);
        mediator.Setup(value => value.CreateRequestClient<GetTokensRequestMessage>(default)).Returns(tokens.Object);
        var context = new Mock<ConsumeContext<ValidateExistingAuthenticationRequestMessage>>();
        context.SetupGet(value => value.CancellationToken).Returns(cancellation.Token);
        context.SetupGet(value => value.Message).Returns(new ValidateExistingAuthenticationRequestMessage(
            "login",
            "password",
            "203.0.113.7",
            ImmutableArray<string>.Empty,
            ImmutableArray.Create("world")));
        var consumer = new ValidateExistingAuthenticationRequestConsumer(mediator.Object);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => consumer.Consume(context.Object));
    }

    [TestMethod]
    public async Task Consume_WhenExistingTokenLookupFails_PropagatesInfrastructureFailure()
    {
        using var cancellation = new CancellationTokenSource();
        var passwordGrant = new Mock<IRequestClient<PasswordGrantCommand>>();
        passwordGrant
            .Setup(client => client.GetResponse<PasswordGrantResult>(
                It.IsAny<PasswordGrantCommand>(),
                cancellation.Token,
                It.IsAny<RequestTimeout>()))
            .ReturnsAsync(CreateResponse(new PasswordGrantResult(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, "42")])))));
        var expected = new InvalidOperationException("token store unavailable");
        var tokens = new Mock<IRequestClient<GetTokensRequestMessage>>();
        tokens
            .Setup(client => client.GetResponse<GetTokensResponseMessage>(
                It.IsAny<GetTokensRequestMessage>(),
                cancellation.Token,
                It.IsAny<RequestTimeout>()))
            .ThrowsAsync(expected);
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default)).Returns(passwordGrant.Object);
        mediator.Setup(value => value.CreateRequestClient<GetTokensRequestMessage>(default)).Returns(tokens.Object);
        var context = new Mock<ConsumeContext<ValidateExistingAuthenticationRequestMessage>>();
        context.SetupGet(value => value.CancellationToken).Returns(cancellation.Token);
        context.SetupGet(value => value.Message).Returns(new ValidateExistingAuthenticationRequestMessage(
            "login",
            "password",
            "203.0.113.7",
            ImmutableArray<string>.Empty,
            ImmutableArray.Create("world")));
        var consumer = new ValidateExistingAuthenticationRequestConsumer(mediator.Object);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => consumer.Consume(context.Object));

        Assert.AreSame(expected, exception);
        passwordGrant.Verify(client => client.GetResponse<PasswordGrantResult>(
            It.IsAny<PasswordGrantCommand>(),
            cancellation.Token,
            It.IsAny<RequestTimeout>()), Times.Once);
        tokens.Verify(client => client.GetResponse<GetTokensResponseMessage>(
            It.IsAny<GetTokensRequestMessage>(),
            cancellation.Token,
            It.IsAny<RequestTimeout>()), Times.Once);
    }

    private static Response<T> CreateResponse<T>(T message) where T : class
    {
        var response = new Mock<Response<T>>();
        response.SetupGet(value => value.Message).Returns(message);
        return response.Object;
    }
}
