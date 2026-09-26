using System.Collections.Concurrent;
using DotNet.Testcontainers.Builders;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Services.GameWorld.Extensions;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.IntegrationTests;

[TestClass]
public sealed class CharacterHydrationEndpointIntegrationTests
{
    [TestMethod]
    [Timeout(120000)]
    public async Task IndependentWorlds_KeepHydrationRepliesWithOwner_AcrossProcessReplacement()
    {
        await using var broker = new ContainerBuilder("masstransit/rabbitmq")
            .WithPortBinding(5672, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete"))
            .Build();
        await broker.StartAsync();
        var address = new Uri($"rabbitmq://{broker.Hostname}:{broker.GetMappedPublicPort(5672)}/");
        var replies = new ConcurrentDictionary<uint, Uri>();
        await using var characters = new ServiceCollection()
            .AddSingleton(replies)
            .AddMassTransit(registration =>
            {
                registration.AddConsumer<CharacterReplyConsumer>();
                registration.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(address);
                    bus.ConfigureEndpoints(context);
                });
            }).BuildServiceProvider(true);
        var firstIdentity = new WorldInstanceIdentity();
        var secondIdentity = new WorldInstanceIdentity();
        await using var first = CreateWorld(address, firstIdentity);
        await using var second = CreateWorld(address, secondIdentity);
        var buses = new[] { characters.GetRequiredService<IBusControl>(), first.GetRequiredService<IBusControl>(), second.GetRequiredService<IBusControl>() };
        try
        {
            foreach (var bus in buses)
                await bus.StartAsync();

            await Task.WhenAll(
                Hydrate(first, firstIdentity, 1, replies),
                Hydrate(second, secondIdentity, 2, replies));
            await Task.WhenAll(
                Hydrate(first, firstIdentity, 3, replies),
                Hydrate(second, secondIdentity, 4, replies));

            await buses[1].StopAsync();
            var replacementIdentity = new WorldInstanceIdentity();
            await using var replacement = CreateWorld(address, replacementIdentity);
            var replacementBus = replacement.GetRequiredService<IBusControl>();
            try
            {
                await replacementBus.StartAsync();
                await Task.WhenAll(
                    Hydrate(replacement, replacementIdentity, 5, replies),
                    Hydrate(second, secondIdentity, 6, replies));
                Assert.AreNotEqual(replies[1], replies[5]);
            }
            finally
            {
                await replacementBus.StopAsync();
            }
        }
        finally
        {
            foreach (var bus in buses.Reverse())
                await bus.StopAsync();
        }
    }

    private static ServiceProvider CreateWorld(Uri address, WorldInstanceIdentity identity) => new ServiceCollection()
        .AddLogging(logging => logging.AddConsole())
        .AddAutoMapper(configuration => configuration.AddProfile<CharacterProfile>())
        .AddMassTransit(registration =>
        {
            registration.AddWorldCharacterHydration(identity);
            registration.AddDelayedMessageScheduler();
            registration.UsingRabbitMq((context, bus) =>
            {
                bus.Host(address);
                bus.UseDelayedMessageScheduler();
                bus.ConfigureEndpoints(context);
            });
        }).BuildServiceProvider(true);

    private static async Task Hydrate(ServiceProvider world, WorldInstanceIdentity identity, uint masterId, ConcurrentDictionary<uint, Uri> replies)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var scope = world.CreateAsyncScope();
        var request = new HydrateCharacter(masterId);
        var response = await scope.ServiceProvider.GetRequiredService<IRequestClient<HydrateCharacter>>()
            .GetResponse<CharacterHydrated>(request, timeout.Token);
        Assert.AreEqual(masterId, response.Message.MasterId);
        Assert.AreEqual(request.CorrelationId, response.Message.CorrelationId);
        Assert.AreEqual(41L, response.Message.SnapshotRevision);
        Assert.AreEqual(3222, response.Message.Details.CoordX);
        Assert.AreEqual($"/hagalaz-gameworld-hydration-{identity.InstanceId}", replies[masterId].AbsolutePath);
    }

    public sealed class CharacterReplyConsumer(ConcurrentDictionary<uint, Uri> replies) : IConsumer<GetCharacterRequest>
    {
        public async Task Consume(ConsumeContext<GetCharacterRequest> context)
        {
            replies[context.Message.MasterId] = context.ResponseAddress!;
            await context.RespondAsync(new GetCharacterResponse(context.Message.CorrelationId, context.Message.MasterId,
                new AppearanceDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                new DetailsDto(3222, 3222, 0),
                new StatisticsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, [], [], [], [], []),
                new ItemCollectionDto { Bank = [], Inventory = [], Equipment = [], FamiliarInventory = [], Rewards = [], MoneyPouch = [] },
                new FamiliarDto(1, 100, true, 100), new MusicDto([], [], false, false),
                new FarmingDto(), new SlayerDto(), new NotesDto(), new ProfileDto { JsonData = "{}" },
                new ItemAppearanceCollectionDto { Appearances = [] }, new StateDto { StatesEx = [] }, 41L));
        }
    }
}
