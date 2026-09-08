using System.Threading;
using Hagalaz.Authorization.Messages;
using Hagalaz.Services.Authorization.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.Authorization.Tests;

[TestClass]
public sealed class RevokeTokenRequestConsumerTests
{
    [TestMethod]
    public async Task Consume_RevokesOnlyTheRequestedAuthorization()
    {
        var authorizationManager = CreateAuthorizationManager("authorization-a", "42", "application-id", Statuses.Valid, true);
        var tokenManager = CreateTokenManager();
        var applicationManager = CreateApplicationManager();
        var context = CreateContext("authorization-a");

        await new RevokeTokenRequestConsumer(tokenManager.Object, applicationManager.Object, authorizationManager.Object)
            .Consume(context.Context.Object);

        Assert.IsTrue(context.Response!.Succeeded);
        tokenManager.Verify(manager => manager.RevokeByAuthorizationIdAsync(
            "authorization-a", It.IsAny<CancellationToken>()), Times.Once);
        authorizationManager.Verify(manager => manager.TryRevokeAsync(
            It.Is<OpenIddictEntityFrameworkCoreAuthorization>(value => value.Id == "authorization-a"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_WhenAuthorizationBelongsToAnotherSubject_DoesNotRevokeAnything()
    {
        var authorizationManager = CreateAuthorizationManager("authorization-a", "99", "application-id", Statuses.Valid, true);
        var tokenManager = CreateTokenManager();
        var applicationManager = CreateApplicationManager();
        var context = CreateContext("authorization-a");

        await new RevokeTokenRequestConsumer(tokenManager.Object, applicationManager.Object, authorizationManager.Object)
            .Consume(context.Context.Object);

        Assert.IsTrue(context.Response!.Succeeded);
        tokenManager.Verify(manager => manager.RevokeByAuthorizationIdAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        authorizationManager.Verify(manager => manager.TryRevokeAsync(
            It.IsAny<OpenIddictEntityFrameworkCoreAuthorization>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_WhenAuthorizationIsUnknown_DoesNotBroadRevokeBySubject()
    {
        var authorizationManager = CreateAuthorizationManager(null, null, null, null, false);
        var tokenManager = CreateTokenManager();
        var applicationManager = CreateApplicationManager();
        var context = CreateContext("missing-authorization");

        await new RevokeTokenRequestConsumer(tokenManager.Object, applicationManager.Object, authorizationManager.Object)
            .Consume(context.Context.Object);

        Assert.IsTrue(context.Response!.Succeeded);
        tokenManager.Verify(manager => manager.RevokeByAuthorizationIdAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        tokenManager.Verify(manager => manager.RevokeBySubjectAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_WhenAuthorizationWasAlreadyRevoked_IsIdempotent()
    {
        var authorizationManager = CreateAuthorizationManager("authorization-a", "42", "application-id", Statuses.Revoked, false);
        var tokenManager = CreateTokenManager();
        var applicationManager = CreateApplicationManager();
        var context = CreateContext("authorization-a");

        await new RevokeTokenRequestConsumer(tokenManager.Object, applicationManager.Object, authorizationManager.Object)
            .Consume(context.Context.Object);

        Assert.IsTrue(context.Response!.Succeeded);
        tokenManager.Verify(manager => manager.RevokeByAuthorizationIdAsync(
            "authorization-a", It.IsAny<CancellationToken>()), Times.Once);
        authorizationManager.Verify(manager => manager.TryRevokeAsync(
            It.IsAny<OpenIddictEntityFrameworkCoreAuthorization>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>> CreateTokenManager()
    {
        var manager = new Mock<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>>(
            new Mock<IOpenIddictTokenCache<OpenIddictEntityFrameworkCoreToken>>().Object,
            NullLogger<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>>.Instance,
            new Mock<IOptionsMonitor<OpenIddictCoreOptions>>().Object,
            new Mock<IOpenIddictTokenStore<OpenIddictEntityFrameworkCoreToken>>().Object);
        manager.Setup(value => value.RevokeByAuthorizationIdAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<long>(1));
        return manager;
    }

    private static Mock<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>> CreateApplicationManager()
    {
        var application = new OpenIddictEntityFrameworkCoreApplication { Id = "application-id" };
        var manager = new Mock<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>(
            new Mock<IOpenIddictApplicationCache<OpenIddictEntityFrameworkCoreApplication>>().Object,
            NullLogger<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>.Instance,
            new Mock<IOptionsMonitor<OpenIddictCoreOptions>>().Object,
            new Mock<IOpenIddictApplicationStore<OpenIddictEntityFrameworkCoreApplication>>().Object);
        manager.Setup(value => value.FindByClientIdAsync("world-client", It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<OpenIddictEntityFrameworkCoreApplication?>(application));
        manager.Setup(value => value.GetIdAsync(application, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>("application-id"));
        return manager;
    }

    private static Mock<OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization>> CreateAuthorizationManager(
        string? authorizationId,
        string? subject,
        string? applicationId,
        string? status,
        bool tryRevoke)
    {
        var authorization = authorizationId == null
            ? null
            : new OpenIddictEntityFrameworkCoreAuthorization { Id = authorizationId };
        var manager = new Mock<OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization>>(
            new Mock<IOpenIddictAuthorizationCache<OpenIddictEntityFrameworkCoreAuthorization>>().Object,
            NullLogger<OpenIddictAuthorizationManager<OpenIddictEntityFrameworkCoreAuthorization>>.Instance,
            new Mock<IOptionsMonitor<OpenIddictCoreOptions>>().Object,
            new Mock<IOpenIddictAuthorizationStore<OpenIddictEntityFrameworkCoreAuthorization>>().Object);
        manager.Setup(value => value.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<OpenIddictEntityFrameworkCoreAuthorization?>(authorization));
        if (authorization != null)
        {
            manager.Setup(value => value.GetSubjectAsync(authorization, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<string?>(subject));
            manager.Setup(value => value.GetApplicationIdAsync(authorization, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<string?>(applicationId));
            manager.Setup(value => value.GetStatusAsync(authorization, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<string?>(status));
            manager.Setup(value => value.TryRevokeAsync(authorization, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<bool>(tryRevoke));
        }
        return manager;
    }

    private static ContextFixture CreateContext(string authorizationId)
    {
        var context = new Moq.Mock<ConsumeContext<RevokeTokenRequestMessage>>();
        var fixture = new ContextFixture(context);
        context.SetupGet(value => value.Message)
            .Returns(new RevokeTokenRequestMessage("world-client", "42", authorizationId));
        context.SetupGet(value => value.CancellationToken).Returns(CancellationToken.None);
        context.Setup(value => value.RespondAsync(It.IsAny<RevokeTokenResponseMessage>()))
            .Callback<RevokeTokenResponseMessage>(message => fixture.Response = message)
            .Returns(Task.CompletedTask);
        return fixture;
    }

    private sealed class ContextFixture
    {
        public ContextFixture(Mock<ConsumeContext<RevokeTokenRequestMessage>> context) => Context = context;
        public Mock<ConsumeContext<RevokeTokenRequestMessage>> Context { get; }
        public RevokeTokenResponseMessage? Response { get; set; }
    }
}
