using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AutoMapper;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldStatusServiceShutdownTests
{
    [TestMethod]
    [Timeout(10000)]
    public async Task Stop_RemovesReadinessFlushesCharactersPublishesMatchingOfflineBeforeBusStops()
    {
        var events = new List<string>();
        var onlinePublished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var offlinePublished = new TaskCompletionSource<WorldOfflineMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bus = Substitute.For<IBus>();
        var mediator = Substitute.For<IGameMediator>();
        var identity = new WorldInstanceIdentity();
        var lifecycle = new WorldLifecycleState();
        lifecycle.MarkCompleted();
        var onlineMessage = CreateOnlineMessage(identity);

#pragma warning disable CA2012
        mediator.GetResponseAsync<WorldStatusRequest, WorldOnlineMessage>(Arg.Any<WorldStatusRequest>())
            .Returns(new ValueTask<WorldOnlineMessage>(onlineMessage));
#pragma warning restore CA2012
        bus.Publish(Arg.Any<WorldOnlineMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                onlinePublished.TrySetResult(true);
                return Task.CompletedTask;
            });
        bus.Publish(Arg.Any<WorldStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        bus.Publish(Arg.Any<WorldOfflineMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var message = callInfo.Arg<WorldOfflineMessage>() ?? throw new InvalidOperationException("Offline status message was not supplied.");
                Assert.IsFalse(events.Contains("bus-stopped"));
                events.Add("offline");
                offlinePublished.TrySetResult(message);
                return Task.CompletedTask;
            });

        var character = Substitute.For<ICharacter>();
        var characterStore = new SingleCharacterStore(character);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.IsPendingLogout(character).Returns(false);
        persistenceService.PersistAsync(character, true, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Assert.IsFalse(events.Contains("bus-stopped"));
                events.Add("character-flush");
                return Task.CompletedTask;
            });

        var busLifetime = new RecordingBusLifetime(events);
        using var host = new HostBuilder()
            .ConfigureServices(collection =>
            {
                collection.AddLogging();
                collection.AddSingleton(bus);
                collection.AddSingleton(mediator);
                collection.AddSingleton<IMapper>(Substitute.For<IMapper>());
                collection.AddSingleton(identity);
                collection.AddSingleton(lifecycle);
                collection.AddSingleton<WorldRegistrationStore>();
                collection.AddSingleton<IOptions<WorldOptions>>(Options.Create(new WorldOptions
                {
                    Id = onlineMessage.Id,
                    Name = onlineMessage.Name,
                    AdvertisedEndpoint = new WorldEndpointOptions { Host = onlineMessage.IpAddress, Port = onlineMessage.Port },
                    RegistrationLeaseDuration = TimeSpan.FromMinutes(1),
                    RegistrationRenewalInterval = TimeSpan.FromMinutes(1),
                    RegistrationRetryDelay = TimeSpan.FromSeconds(1)
                }));
                collection.AddSingleton<ICharacterStore>(characterStore);
                collection.AddScoped<ICharacterPersistenceService>(_ => persistenceService);
                collection.AddSingleton(busLifetime);
                collection.AddSingleton<IHostedService>(provider => provider.GetRequiredService<RecordingBusLifetime>());
                collection.AddHostedService<WorldStatusService>();
                collection.AddHostedService<CharacterDehydrationWorkerService>();
            })
            .Build();

        await host.StartAsync();
        await onlinePublished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(lifecycle.CanAcceptWorldSignIns);

        await host.StopAsync();

        var offline = await offlinePublished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(onlineMessage.Id, offline.Id);
        Assert.AreEqual(identity.InstanceId, offline.InstanceId);
        Assert.AreEqual(identity.Generation, offline.Generation);
        CollectionAssert.AreEqual(new[] { "character-flush", "offline", "bus-stopped" }, events);
        Assert.IsFalse(lifecycle.CanAcceptWorldSignIns);
    }

    private static WorldOnlineMessage CreateOnlineMessage(WorldInstanceIdentity identity) => new()
    {
        Id = 1,
        Name = "World 1",
        IpAddress = "127.0.0.1",
        Port = 443,
        CharacterCount = 0,
        InstanceId = identity.InstanceId,
        Generation = identity.Generation,
        StartedAt = identity.StartedAt,
        LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1),
        Settings = new WorldOnlineMessage.WorldSettings
        {
            IsMembersOnly = true,
            IsQuickChatEnabled = false,
            IsPvP = false,
            IsLootShareEnabled = false,
            IsHighLighted = false
        },
        Location = new WorldOnlineMessage.WorldLocation { Name = "Local", Flag = 0 }
    };

    private sealed class RecordingBusLifetime : IHostedService
    {
        private readonly List<string> _events;

        public RecordingBusLifetime(List<string> events) => _events = events;

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _events.Add("bus-stopped");
            return Task.CompletedTask;
        }
    }

    private sealed class SingleCharacterStore : ICharacterStore
    {
        private readonly ICharacter _character;

        public SingleCharacterStore(ICharacter character) => _character = character;

        public async IAsyncEnumerable<ICharacter> FindAllAsync()
        {
            yield return _character;
            await Task.CompletedTask;
        }

        public ValueTask<int> CountAsync() => throw new NotSupportedException();
        public ValueTask<bool> AddAsync(ICharacter character) => throw new NotSupportedException();
        public ValueTask<bool> RemoveAsync(ICharacter character) => throw new NotSupportedException();
        public ValueTask<ICharacter?> FindAsync(Func<ICharacter, bool> predicate) => throw new NotSupportedException();
        public ValueTask<ICharacter?> FindByIdAsync(uint id) => throw new NotSupportedException();
    }
}
