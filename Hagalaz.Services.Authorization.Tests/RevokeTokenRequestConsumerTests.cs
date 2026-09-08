using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task Consume_WhenConcurrentRevokeAlreadyMadeTokenInvalid_ReturnsSuccess()
    {
        var token = new OpenIddictEntityFrameworkCoreToken { Status = Statuses.Valid };
        var tokenManager = CreateTokenManager(token, tryRevoke: false, statusAfterFailure: Statuses.Revoked);
        var applicationManager = CreateApplicationManager();
        var context = CreateContext(DateTime.UtcNow);

        await new RevokeTokenRequestConsumer(tokenManager.Object, applicationManager.Object).Consume(context.Context.Object);

        Assert.IsTrue(context.Response!.Succeeded);
        Assert.IsNull(context.Response.Error);
        tokenManager.Verify(manager => manager.FindAsync(
            "42", "application-id", Statuses.Valid, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_WhenFailedRevokeLeavesTokenValid_ReturnsRevocationFailure()
    {
        var token = new OpenIddictEntityFrameworkCoreToken { Status = Statuses.Valid };
        var tokenManager = CreateTokenManager(token, tryRevoke: false, statusAfterFailure: Statuses.Valid);
        var applicationManager = CreateApplicationManager();
        var context = CreateContext(DateTime.UtcNow);

        await new RevokeTokenRequestConsumer(tokenManager.Object, applicationManager.Object).Consume(context.Context.Object);

        Assert.IsFalse(context.Response!.Succeeded);
        Assert.AreEqual(OpenIddictResources.ID2079, context.Response.Error);
    }

    [TestMethod]
    public async Task Consume_WhenTokenWasCreatedAfterLogoutStarted_DoesNotRevokeIt()
    {
        var logoutStartedAt = DateTime.UtcNow;
        var token = new OpenIddictEntityFrameworkCoreToken
        {
            Status = Statuses.Valid,
            CreationDate = logoutStartedAt.AddSeconds(1)
        };
        var tokenManager = CreateTokenManager(token, tryRevoke: false, statusAfterFailure: Statuses.Valid);
        var applicationManager = CreateApplicationManager();
        var context = CreateContext(logoutStartedAt);

        await new RevokeTokenRequestConsumer(tokenManager.Object, applicationManager.Object).Consume(context.Context.Object);

        Assert.IsTrue(context.Response!.Succeeded);
        tokenManager.Verify(manager => manager.TryRevokeAsync(token, It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>> CreateTokenManager(
        OpenIddictEntityFrameworkCoreToken token,
        bool tryRevoke,
        string statusAfterFailure)
    {
        var cache = new Mock<IOpenIddictTokenCache<OpenIddictEntityFrameworkCoreToken>>();
        var store = new Mock<IOpenIddictTokenStore<OpenIddictEntityFrameworkCoreToken>>();
        var options = new Mock<IOptionsMonitor<OpenIddictCoreOptions>>();
        var manager = new Mock<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>>(
            cache.Object,
            NullLogger<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>>.Instance,
            options.Object,
            store.Object);
        manager.Setup(value => value.FindAsync(
                "42", "application-id", Statuses.Valid, null, It.IsAny<CancellationToken>()))
            .Returns(Enumerate(token));
        manager.Setup(value => value.TryRevokeAsync(token, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<bool>(tryRevoke));
        manager.Setup(value => value.GetStatusAsync(token, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(statusAfterFailure));
        return manager;
    }

    private static Mock<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>> CreateApplicationManager()
    {
        var application = new OpenIddictEntityFrameworkCoreApplication { Id = "application-id" };
        var cache = new Mock<IOpenIddictApplicationCache<OpenIddictEntityFrameworkCoreApplication>>();
        var store = new Mock<IOpenIddictApplicationStore<OpenIddictEntityFrameworkCoreApplication>>();
        var options = new Mock<IOptionsMonitor<OpenIddictCoreOptions>>();
        var manager = new Mock<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>(
            cache.Object,
            NullLogger<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>.Instance,
            options.Object,
            store.Object);
        manager.Setup(value => value.FindByClientIdAsync("world-client", It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<OpenIddictEntityFrameworkCoreApplication?>(application));
        manager.Setup(value => value.GetIdAsync(application, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>("application-id"));
        return manager;
    }

    private static ContextFixture CreateContext(DateTime tokenCreatedBefore)
    {
        var context = new Mock<ConsumeContext<RevokeTokenRequestMessage>>();
        var fixture = new ContextFixture(context);
        context.SetupGet(value => value.Message)
            .Returns(new RevokeTokenRequestMessage("world-client", "42", tokenCreatedBefore));
        context.SetupGet(value => value.CancellationToken).Returns(CancellationToken.None);
        context.Setup(value => value.RespondAsync(It.IsAny<RevokeTokenResponseMessage>()))
            .Callback<RevokeTokenResponseMessage>(message => fixture.Response = message)
            .Returns(Task.CompletedTask);
        return fixture;
    }

    private static async IAsyncEnumerable<T> Enumerate<T>(T item)
    {
        yield return item;
        await Task.CompletedTask;
    }

    private sealed class ContextFixture
    {
        public ContextFixture(Mock<ConsumeContext<RevokeTokenRequestMessage>> context) => Context = context;

        public Mock<ConsumeContext<RevokeTokenRequestMessage>> Context { get; }
        public RevokeTokenResponseMessage? Response { get; set; }
    }
}
