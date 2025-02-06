using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Logic.Characters.StateMachines;
using Hagalaz.Services.GameWorld.Logic.Characters.States;
using Hagalaz.Services.GameWorld.Profiles;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CharacterHydrationStateMachineTests
    {
        [TestMethod]
        public async Task Should_hydrate_character()
        {
            await using var provider = new ServiceCollection()
            .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<CharacterRequestConsumer>();
                x.AddSagaStateMachine<CharacterHydrationStateMachine, CharacterHydrationState>();
            })
            .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var client = harness.GetRequestClient<HydrateCharacter>();
            var response = await client.GetResponse<CharacterHydrated>(new HydrateCharacter(1));

            Assert.IsTrue(await harness.Sent.Any<CharacterHydrated>());
            Assert.IsTrue(await harness.Consumed.Any<HydrateCharacter>());
            Assert.IsNotNull(response.Message.Details);
            Assert.IsNotNull(response.Message.Statistics);
            Assert.IsNotNull(response.Message.ItemCollection);
            Assert.IsNotNull(response.Message.Appearance);
            Assert.IsNotNull(response.Message.Appearance);

            var consumerHarness = harness.GetConsumerHarness<CharacterRequestConsumer>();

            Assert.IsTrue(await consumerHarness.Consumed.Any<GetCharacterRequest>());

            await harness.Stop();
        }

        [TestMethod]
        public async Task Should_not_hydrate_notfound_character()
        {
            await using var provider = new ServiceCollection()
            .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<CharacterNotFoundRequestConsumer>();
                x.AddSagaStateMachine<CharacterHydrationStateMachine, CharacterHydrationState>();
            })
            .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var client = harness.GetRequestClient<HydrateCharacter>();
            var response = await client.GetResponse<CharacterHydrated, CharacterNotFound>(new HydrateCharacter(1));

            Assert.IsTrue(await harness.Sent.Any<CharacterNotFound>());
            Assert.IsTrue(await harness.Consumed.Any<HydrateCharacter>());

            var consumerHarness = harness.GetConsumerHarness<CharacterNotFoundRequestConsumer>();

            Assert.IsTrue(await consumerHarness.Consumed.Any<GetCharacterRequest>());

            await harness.Stop();
        }

        [TestMethod]
        public async Task Should_handle_multiple_hydrate_requests()
        {
            await using var provider = new ServiceCollection()
                .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
                .AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<CharacterRequestConsumer>();
                    x.AddSagaStateMachine<CharacterHydrationStateMachine, CharacterHydrationState>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var client = harness.GetRequestClient<HydrateCharacter>();

            var response1 = await client.GetResponse<CharacterHydrated>(new HydrateCharacter(1));
            var response2 = await client.GetResponse<CharacterHydrated>(new HydrateCharacter(2));

            Assert.IsTrue(await harness.Sent.Any<CharacterHydrated>());
            Assert.IsTrue(await harness.Consumed.Any<HydrateCharacter>());
            Assert.IsNotNull(response1.Message.Details);
            Assert.IsNotNull(response2.Message.Details);

            await harness.Stop();
        }

        class CharacterRequestConsumer : IConsumer<GetCharacterRequest>
        {
            public async Task Consume(ConsumeContext<GetCharacterRequest> context)
            {
                var message = context.Message;
                await context.RespondAsync(new GetCharacterResponse(message.CorrelationId, message.MasterId, 
                    new AppearanceDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ,0, 0),
                    new DetailsDto(3222, 3222, 0), 
                    new StatisticsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<bool>(), Array.Empty<int>(), Array.Empty<double>()), 
                    new ItemCollectionDto 
                    { 
                        Bank = new List<ItemDto>(),
                        Inventory = new List<ItemDto>(),
                        Equipment = new List<ItemDto>(),
                        FamiliarInventory = new List<ItemDto>(),
                        Rewards = new List<ItemDto>(),
                        MoneyPouch = new List<ItemDto>(),
                       
                    }, 
                    new FamiliarDto(1, 100, true, 100), 
                    new MusicDto(Array.Empty<int>(), Array.Empty<int>(), false, false),
                    new FarmingDto(), 
                    new SlayerDto(),
                    new NotesDto(), 
                    new ProfileDto() { JsonData = string.Empty }, new ItemAppearanceCollectionDto { Appearances = new List<ItemAppearanceDto>() }, 
                    new StateDto { StatesEx = new List<StateDto.StateExDto>() }));
            }
        }

        class CharacterNotFoundRequestConsumer : IConsumer<GetCharacterRequest>
        {
            public async Task Consume(ConsumeContext<GetCharacterRequest> context)
            {
                var message = context.Message;
                await context.RespondAsync(new CharacterNotFound(message.CorrelationId, message.MasterId));
            }
        }
    }
}
