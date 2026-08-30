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
public sealed class ValidateUserCredentialsRequestConsumerTests
{
    [TestMethod]
    public async Task Consume_UsesPasswordGrantAndReturnsTheAuthenticatedSubject()
    {
        var passwordGrant = new Mock<IRequestClient<PasswordGrantCommand>>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(OpenIddictConstants.Claims.Subject, "42")],
            "password"));
        passwordGrant
            .Setup(client => client.GetResponse<PasswordGrantResult>(
                It.Is<PasswordGrantCommand>(command =>
                    command.Login == "login" &&
                    command.Password == "password" &&
                    command.Scopes.IsDefaultOrEmpty),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResponse(new PasswordGrantResult(principal)));

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(value => value.CreateRequestClient<PasswordGrantCommand>())
            .Returns(passwordGrant.Object);

        var response = default(ValidateUserCredentialsResponseMessage);
        var consumeContext = new Mock<ConsumeContext<ValidateUserCredentialsRequestMessage>>();
        consumeContext.SetupGet(value => value.Message)
            .Returns(new ValidateUserCredentialsRequestMessage("login", "password"));
        consumeContext
            .Setup(value => value.RespondAsync(It.IsAny<ValidateUserCredentialsResponseMessage>()))
            .Callback<ValidateUserCredentialsResponseMessage>(value => response = value)
            .Returns(Task.CompletedTask);

        await new ValidateUserCredentialsRequestConsumer(mediator.Object).Consume(consumeContext.Object);

        Assert.IsNotNull(response);
        Assert.IsTrue(response.Succeeded);
        Assert.AreEqual("42", response.Subject);
        passwordGrant.Verify(client => client.GetResponse<PasswordGrantResult>(
            It.IsAny<PasswordGrantCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_PreservesExistingCredentialValidationOutcome()
    {
        var passwordGrant = new Mock<IRequestClient<PasswordGrantCommand>>();
        passwordGrant
            .Setup(client => client.GetResponse<PasswordGrantResult>(
                It.IsAny<PasswordGrantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResponse(PasswordGrantResult.CredentialsInvalid));

        var mediator = new Mock<IMediator>();
        mediator
            .Setup(value => value.CreateRequestClient<PasswordGrantCommand>())
            .Returns(passwordGrant.Object);

        var response = default(ValidateUserCredentialsResponseMessage);
        var consumeContext = new Mock<ConsumeContext<ValidateUserCredentialsRequestMessage>>();
        consumeContext.SetupGet(value => value.Message)
            .Returns(new ValidateUserCredentialsRequestMessage("login", "wrong"));
        consumeContext
            .Setup(value => value.RespondAsync(It.IsAny<ValidateUserCredentialsResponseMessage>()))
            .Callback<ValidateUserCredentialsResponseMessage>(value => response = value)
            .Returns(Task.CompletedTask);

        await new ValidateUserCredentialsRequestConsumer(mediator.Object).Consume(consumeContext.Object);

        Assert.IsNotNull(response);
        Assert.IsFalse(response.Succeeded);
        Assert.IsTrue(response.AreCredentialsInvalid);
        Assert.IsNull(response.Subject);
    }

    private static Response<T> CreateResponse<T>(T message)
        where T : class
    {
        var response = new Mock<Response<T>>();
        response.SetupGet(value => value.Message).Returns(message);
        return response.Object;
    }
}
