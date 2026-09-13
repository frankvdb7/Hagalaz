using System.Collections.Immutable;
using System.Security.Claims;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Consumers;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using Hagalaz.Services.Authorization.Services;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Hagalaz.Services.Authorization.Tests;

[TestClass]
public sealed class SignInUserRequestConsumerTests
{
    [TestMethod]
    public async Task Consume_WhenCredentialsAreInvalid_ReturnsCredentialFailureWithoutCreatingAuthorization()
    {
        var passwordGrant = new Mock<IRequestClient<PasswordGrantCommand>>();
        passwordGrant
            .Setup(client => client.GetResponse<PasswordGrantResult>(
                It.IsAny<PasswordGrantCommand>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(CreateResponse(PasswordGrantResult.CredentialsInvalid));
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default)).Returns(passwordGrant.Object);
        var openIddict = new Mock<IOpenIddictService>();
        var applicationManager = CreateApplicationManager().Object;
        var authorizationManager = CreateAuthorizationManager().Object;
        var context = CreateContext();

        var consumer = new SignInUserRequestConsumer(
            mediator.Object,
            openIddict.Object,
            applicationManager,
            authorizationManager,
            CreateTokenManager().Object,
            NullLogger<SignInUserRequestConsumer>.Instance);

        await consumer.Consume(context.Context.Object);

        Assert.IsNotNull(context.Response);
        Assert.IsFalse(context.Response!.Succeeded);
        Assert.IsTrue(context.Response.AreCredentialsInvalid);
        openIddict.Verify(service => service.CreateTransactionAsync(), Times.Never);
        Mock.Get(authorizationManager).Verify(manager => manager.CreateAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<ImmutableArray<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_WhenCredentialsAreValid_ReturnsAuthorizationIdAndAssociatesItWithPrincipal()
    {
        var passwordGrant = CreateSuccessfulPasswordGrant();
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default)).Returns(passwordGrant.Object);
        var openIddict = CreateSuccessfulOpenIddictService();
        var applicationManager = CreateApplicationManager();
        var authorizationManager = CreateAuthorizationManager();
        var context = CreateContext();

        var consumer = new SignInUserRequestConsumer(
            mediator.Object,
            openIddict.Object,
            applicationManager.Object,
            authorizationManager.Object,
            CreateTokenManager().Object,
            NullLogger<SignInUserRequestConsumer>.Instance);

        await consumer.Consume(context.Context.Object);

        Assert.IsNotNull(context.Response);
        Assert.IsTrue(context.Response!.Succeeded);
        Assert.AreEqual("authorization-id", context.Response.AuthorizationId);
        Assert.AreEqual("42", context.Response.Subject);
        openIddict.Verify(service => service.DispatchAsync(It.Is<ProcessSignInContext>(value =>
            value.Principal!.GetAuthorizationId() == "authorization-id")), Times.Once);
        authorizationManager.Verify(manager => manager.CreateAsync(
            It.Is<ClaimsPrincipal>(principal => principal.FindFirst(OpenIddictConstants.Claims.Subject)!.Value == "42"),
            "42", "application-id", OpenIddictConstants.AuthorizationTypes.AdHoc,
            It.IsAny<ImmutableArray<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_WhenTokenGenerationFails_DeletesTheOrphanAuthorization()
    {
        var passwordGrant = CreateSuccessfulPasswordGrant();
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default)).Returns(passwordGrant.Object);
        var openIddict = CreateSuccessfulOpenIddictService();
        openIddict
            .Setup(service => service.DispatchAsync(It.IsAny<ProcessSignInContext>()))
            .ThrowsAsync(new InvalidOperationException("token generation failed"));
        var applicationManager = CreateApplicationManager();
        var authorizationManager = CreateAuthorizationManager();
        var tokenManager = CreateTokenManager();
        var context = CreateContext();
        var consumer = new SignInUserRequestConsumer(
            mediator.Object,
            openIddict.Object,
            applicationManager.Object,
            authorizationManager.Object,
            tokenManager.Object,
            NullLogger<SignInUserRequestConsumer>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => consumer.Consume(context.Context.Object));

        authorizationManager.Verify(manager => manager.DeleteAsync(
            It.Is<OpenIddictEntityFrameworkCoreAuthorization>(authorization => authorization.Id == "authorization-id"),
            It.Is<CancellationToken>(token => !token.IsCancellationRequested)), Times.Once);
        tokenManager.Verify(manager => manager.RevokeByAuthorizationIdAsync(
            "authorization-id", It.Is<CancellationToken>(token => !token.IsCancellationRequested)), Times.Once);
    }

    [TestMethod]
    public async Task Consume_WhenAuthorizationIdLookupFails_DeletesTheCreatedAuthorization()
    {
        var passwordGrant = CreateSuccessfulPasswordGrant();
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default)).Returns(passwordGrant.Object);
        var openIddict = CreateSuccessfulOpenIddictService();
        var applicationManager = CreateApplicationManager();
        var authorizationManager = CreateAuthorizationManager();
        authorizationManager
            .Setup(manager => manager.GetIdAsync(
                It.IsAny<OpenIddictEntityFrameworkCoreAuthorization>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>((string?)null));
        var tokenManager = CreateTokenManager();
        var context = CreateContext();
        var consumer = new SignInUserRequestConsumer(
            mediator.Object,
            openIddict.Object,
            applicationManager.Object,
            authorizationManager.Object,
            tokenManager.Object,
            NullLogger<SignInUserRequestConsumer>.Instance);

        await consumer.Consume(context.Context.Object);

        Assert.IsNotNull(context.Response);
        Assert.IsFalse(context.Response!.Succeeded);
        authorizationManager.Verify(manager => manager.DeleteAsync(
            It.Is<OpenIddictEntityFrameworkCoreAuthorization>(authorization => authorization.Id == "authorization-id"),
            It.Is<CancellationToken>(token => !token.IsCancellationRequested)), Times.Once);
        tokenManager.Verify(manager => manager.RevokeByAuthorizationIdAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        openIddict.Verify(service => service.DispatchAsync(It.IsAny<ProcessSignInContext>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_WhenDispatchFailsAfterRequestCancellation_CleansUpWithNonCanceledToken()
    {
        var passwordGrant = CreateSuccessfulPasswordGrant();
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.CreateRequestClient<PasswordGrantCommand>(default)).Returns(passwordGrant.Object);
        var openIddict = CreateSuccessfulOpenIddictService();
        openIddict
            .Setup(service => service.DispatchAsync(It.IsAny<ProcessSignInContext>()))
            .ThrowsAsync(new InvalidOperationException("token generation failed"));
        var applicationManager = CreateApplicationManager();
        var authorizationManager = CreateAuthorizationManager();
        var tokenManager = CreateTokenManager();
        var context = CreateContext(new CancellationToken(canceled: true));
        var consumer = new SignInUserRequestConsumer(
            mediator.Object,
            openIddict.Object,
            applicationManager.Object,
            authorizationManager.Object,
            tokenManager.Object,
            NullLogger<SignInUserRequestConsumer>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => consumer.Consume(context.Context.Object));

        tokenManager.Verify(manager => manager.RevokeByAuthorizationIdAsync(
            "authorization-id", It.Is<CancellationToken>(token => !token.IsCancellationRequested)), Times.Once);
        authorizationManager.Verify(manager => manager.DeleteAsync(
            It.Is<OpenIddictEntityFrameworkCoreAuthorization>(authorization => authorization.Id == "authorization-id"),
            It.Is<CancellationToken>(token => !token.IsCancellationRequested)), Times.Once);
    }

    private static Mock<IRequestClient<PasswordGrantCommand>> CreateSuccessfulPasswordGrant()
    {
        var passwordGrant = new Mock<IRequestClient<PasswordGrantCommand>>();
        passwordGrant
            .Setup(client => client.GetResponse<PasswordGrantResult>(
                It.IsAny<PasswordGrantCommand>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(CreateResponse(new PasswordGrantResult(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, "42")])))));
        return passwordGrant;
    }

    private static Mock<IOpenIddictService> CreateSuccessfulOpenIddictService()
    {
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
        return openIddict;
    }

    private static Mock<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>> CreateApplicationManager()
    {
        var application = new OpenIddictEntityFrameworkCoreApplication { Id = "application-id" };
        var manager = new Mock<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>(
            new Mock<IOpenIddictApplicationCache<OpenIddictEntityFrameworkCoreApplication>>().Object,
            NullLogger<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>.Instance,
            new Mock<Microsoft.Extensions.Options.IOptionsMonitor<OpenIddictCoreOptions>>().Object,
            new Mock<IOpenIddictApplicationStore<OpenIddictEntityFrameworkCoreApplication>>().Object);
        manager.Setup(value => value.FindByClientIdAsync("game-client", It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<OpenIddictEntityFrameworkCoreApplication?>(application));
        manager.Setup(value => value.GetIdAsync(application, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>("application-id"));
        return manager;
    }

    private static Mock<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>> CreateTokenManager()
    {
        var manager = new Mock<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>>(
            new Mock<IOpenIddictTokenCache<OpenIddictEntityFrameworkCoreToken>>().Object,
            NullLogger<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>>.Instance,
            new Mock<Microsoft.Extensions.Options.IOptionsMonitor<OpenIddictCoreOptions>>().Object,
            new Mock<IOpenIddictTokenStore<OpenIddictEntityFrameworkCoreToken>>().Object);
        manager.Setup(value => value.RevokeByAuthorizationIdAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<long>(1));
        return manager;
    }

    private static Mock<OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization>> CreateAuthorizationManager()
    {
        var authorization = new OpenIddictEntityFrameworkCoreAuthorization { Id = "authorization-id" };
        var manager = new Mock<OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization>>(
            new Mock<IOpenIddictAuthorizationCache<OpenIddictEntityFrameworkCoreAuthorization>>().Object,
            NullLogger<OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization>>.Instance,
            new Mock<Microsoft.Extensions.Options.IOptionsMonitor<OpenIddictCoreOptions>>().Object,
            new Mock<IOpenIddictAuthorizationStore<OpenIddictEntityFrameworkCoreAuthorization>>().Object);
        manager.Setup(value => value.CreateAsync(
                It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ImmutableArray<string>>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<OpenIddictEntityFrameworkCoreAuthorization>(authorization));
        manager.Setup(value => value.GetIdAsync(authorization, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>("authorization-id"));
        manager.Setup(value => value.DeleteAsync(authorization, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return manager;
    }

    private static ContextFixture CreateContext(CancellationToken cancellationToken = default)
    {
        var context = new Mock<ConsumeContext<SignInUserRequestMessage>>();
        var fixture = new ContextFixture(context);
        context.SetupGet(value => value.Message).Returns(CreateRequest());
        context.SetupGet(value => value.CancellationToken).Returns(cancellationToken);
        context.Setup(value => value.RespondAsync(It.IsAny<SignInUserResponseMessage>()))
            .Callback<SignInUserResponseMessage>(message => fixture.Response = message)
            .Returns(Task.CompletedTask);
        return fixture;
    }

    private static Response<T> CreateResponse<T>(T message) where T : class
    {
        var response = new Mock<Response<T>>();
        response.SetupGet(value => value.Message).Returns(message);
        return response.Object;
    }

    private static SignInUserRequestMessage CreateRequest() => new(
        "login", "password", "203.0.113.7", "game-client",
        ImmutableArray.Create("openid"), ImmutableArray.Create("game-client"));

    private sealed class ContextFixture
    {
        public ContextFixture(Mock<ConsumeContext<SignInUserRequestMessage>> context) => Context = context;
        public Mock<ConsumeContext<SignInUserRequestMessage>> Context { get; }
        public SignInUserResponseMessage? Response { get; set; }
    }
}
