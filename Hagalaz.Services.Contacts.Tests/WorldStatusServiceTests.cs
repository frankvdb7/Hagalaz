using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Services.Model;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public sealed class WorldStatusServiceTests
{
    [TestMethod]
    [Timeout(5000)]
    public async Task ExpiredWorldSession_IsCleanedUpAfterStatusRequest()
    {
        const int worldId = 1;
        const uint masterId = 100;
        var started = new CancellationTokenSource();
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(x => x.ApplicationStarted).Returns(started.Token);

        var bus = new Mock<IBus>();
        bus.Setup(x => x.Publish(It.IsAny<WorldStatusRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        bus.Setup(x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var characterService = new Mock<ICharacterService>();
        characterService.Setup(x => x.FindCharacterByIdAsync(masterId)).ReturnsAsync(new CharacterDto
        {
            MasterId = masterId,
            DisplayName = "TestUser"
        });

        var contactSessions = new ContactSessionStore();
        contactSessions.TryAdd(masterId, new ContactSessionContext(masterId, worldId, "World 1"));
        var worldSessions = new WorldSessionStore();
        worldSessions.ObserveOnline(new WorldSessionContext(
            worldId,
            "World 1",
            "instance-a",
            1,
            DateTimeOffset.UtcNow.AddMilliseconds(50)));

        var localizer = new Mock<IStringLocalizer<ContactSessionService>>();
        var contactSessionService = new ContactSessionService(
            characterService.Object,
            contactSessions,
            worldSessions,
            bus.Object,
            localizer.Object);
        using var serviceProvider = new ServiceCollection()
            .AddScoped<IContactSessionService>(_ => contactSessionService)
            .BuildServiceProvider();
        var service = new WorldStatusService(
            bus.Object,
            new Mock<ILogger<WorldStatusService>>().Object,
            lifetime.Object,
            worldSessions,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        await service.StartAsync(CancellationToken.None);
        started.Cancel();
        await Task.Delay(TimeSpan.FromMilliseconds(1250));
        await service.StopAsync(CancellationToken.None);

        Assert.IsFalse(worldSessions.TryGetValue(worldId, out _));
        bus.Verify(x => x.Publish(It.IsAny<WorldStatusRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        bus.Verify(x => x.Publish(
                It.Is<ContactSignOutMessage>(message => message.Contact.MasterId == masterId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.IsFalse(contactSessions.TryGetValue(masterId, out _));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task CleanupFailure_DoesNotStopSubsequentExpiryProcessing()
    {
        var started = new CancellationTokenSource();
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(x => x.ApplicationStarted).Returns(started.Token);

        var bus = new Mock<IBus>();
        bus.Setup(x => x.Publish(It.IsAny<WorldStatusRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("status request failed"));

        var cleanupService = new Mock<IContactSessionService>();
        var cleanupCalls = 0;
        cleanupService
            .Setup(x => x.RemoveWorldSessions(It.IsAny<int>()))
            .Returns((int _) =>
                Interlocked.Increment(ref cleanupCalls) == 1
                    ? Task.FromException(new TimeoutException("cleanup failed"))
                    : Task.CompletedTask);

        var worldSessions = new WorldSessionStore();
        var expiresAt = DateTimeOffset.UtcNow.AddMilliseconds(50);
        worldSessions.ObserveOnline(new WorldSessionContext(1, "World 1", "instance-a", 1, expiresAt));
        worldSessions.ObserveOnline(new WorldSessionContext(2, "World 2", "instance-b", 1, expiresAt));

        using var serviceProvider = new ServiceCollection()
            .AddScoped<IContactSessionService>(_ => cleanupService.Object)
            .BuildServiceProvider();
        var service = new WorldStatusService(
            bus.Object,
            new Mock<ILogger<WorldStatusService>>().Object,
            lifetime.Object,
            worldSessions,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        await service.StartAsync(CancellationToken.None);
        started.Cancel();
        await Task.Delay(TimeSpan.FromMilliseconds(1250));
        await service.StopAsync(CancellationToken.None);

        Assert.AreEqual(2, cleanupCalls);
        cleanupService.Verify(x => x.RemoveWorldSessions(1), Times.Once);
        cleanupService.Verify(x => x.RemoveWorldSessions(2), Times.Once);
    }
}
