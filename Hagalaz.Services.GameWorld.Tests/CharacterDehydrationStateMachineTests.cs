using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Logic.Characters.StateMachines;
using Hagalaz.Services.GameWorld.Logic.Characters.States;
using Hagalaz.Services.GameWorld.Profiles;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CharacterDehydrationStateMachineTests
    {
        private HydratedAppearanceDto _appearanceMock = default!;
        private HydratedDetailsDto _detailsMock = default!;
        private HydratedStatisticsDto _statisticsMock = default!;
        private HydratedItemCollectionDto _itemsMock = default!;
        private HydratedFamiliarDto _familiarMock = default!;
        private HydratedMusicDto _musicMock = default!;
        private HydratedFarmingDto _farmingMock = default!;
        private HydratedSlayerDto _slayerMock = default!;
        private HydratedNotesDto _notesMock = default!;
        private HydratedProfileDto _profileMock = default!;
        private HydratedItemAppearanceCollectionDto _itemAppearanceCollectionMock = default!;
        private HydratedStateDto _stateMock = default!;

        [TestInitialize]
        public void Initialize()
        {
            _appearanceMock = new HydratedAppearanceDto
            {
                ArmsLook = 0,
                BeardLook = 0,
                FeetColor = 0,
                FeetLook = 0,
                Gender = 1,
                HairColor = 0,
                HairLook = 0,
                LegColor = 0,
                LegsLook = 0,
                SkinColor = 0,
                TorsoColor = 0,
                TorsoLook = 0,
                WristLook = 0,
                DisplayTitle = DisplayTitle.None
            };
            _detailsMock = new HydratedDetailsDto
            {
                CoordX = 3222,
                CoordY = 3222,
                CoordZ = 0
            };
            _statisticsMock = new HydratedStatisticsDto
            {
                AttackLevel = 0,
                AttackExp = 0,
                DefenceLevel = 0,
                DefenceExp = 0,
                StrengthLevel = 0,
                StrengthExp = 0,
                ConstitutionLevel = 0,
                ConstitutionExp = 0,
                RangeLevel = 0,
                RangeExp = 0,
                PrayerLevel = 0,
                PrayerExp = 0,
                MagicLevel = 0,
                MagicExp = 0,
                CookingLevel = 0,
                CookingExp = 0,
                WoodcuttingLevel = 0,
                WoodcuttingExp = 0,
                FletchingLevel = 0,
                FletchingExp = 0,
                FishingLevel = 0,
                FishingExp = 0,
                FiremakingLevel = 0,
                FiremakingExp = 0,
                CraftingLevel = 0,
                CraftingExp = 0,
                SmithingLevel = 0,
                SmithingExp = 0,
                MiningLevel = 0,
                MiningExp = 0,
                HerbloreLevel = 0,
                HerbloreExp = 0,
                AgilityLevel = 0,
                AgilityExp = 0,
                ThievingLevel = 0,
                ThievingExp = 0,
                SlayerLevel = 0,
                SlayerExp = 0,
                FarmingLevel = 0,
                FarmingExp = 0,
                RunecraftingLevel = 0,
                RunecraftingExp = 0,
                ConstructionLevel = 0,
                ConstructionExp = 0,
                HunterLevel = 0,
                HunterExp = 0,
                SummoningLevel = 0,
                SummoningExp = 0,
                DungeoneeringLevel = 0,
                DungeoneeringExp = 0,
                LifePoints = 0,
                PrayerPoints = 0,
                RunEnergy = 0,
                SpecialEnergy = 0,
                PoisonAmount = 0,
                PlayTime = 0,
                XpCounters = Array.Empty<int>(),
                TrackedXpCounters = Array.Empty<int>(),
                EnabledXpCounters = Array.Empty<bool>(),
                TargetSkillLevels = Array.Empty<int>(),
                TargetSkillExperiences = Array.Empty<double>()
            };

            _itemsMock = new HydratedItemCollectionDto() 
            { 
                Bank = new List<HydratedItemDto>(),
                Equipment = new List<HydratedItemDto>(), 
                Inventory = new List<HydratedItemDto>(), 
                FamiliarInventory = new List<HydratedItemDto>(), 
                MoneyPouch = new List<HydratedItemDto>(), 
                Rewards = new List<HydratedItemDto>() 
            };

            _familiarMock = new HydratedFamiliarDto()
            {
                FamiliarId = 1337
            };

            _musicMock = new HydratedMusicDto([], [], false, false);

            _farmingMock = new HydratedFarmingDto()
            {
                Patches = []
            };

            _slayerMock = new HydratedSlayerDto()
            {
                Task = new HydratedSlayerDto.SlayerTaskDto() { Id = 1337 }
            };

            _notesMock = new HydratedNotesDto()
            {
                Notes = []
            };

            _profileMock = new HydratedProfileDto()
            {
                JsonData = "{}"
            };

            _itemAppearanceCollectionMock = new HydratedItemAppearanceCollectionDto()
            {
                Appearances = []
            };

            _stateMock = new HydratedStateDto()
            {
                StatesEx = []
            };
        }

        [TestMethod]
        public async Task Should_dehydrate_character()
        {
            await using var provider = new ServiceCollection()
            .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<UpdateCharacterRequestConsumer>();
                x.AddSagaStateMachine<CharacterDehydrationStateMachine, CharacterDehydrationState>();
            })
            .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var client = harness.GetRequestClient<DehydrateCharacter>();
            var response = await client.GetResponse<CharacterDehydrated>(new DehydrateCharacter
            { 
                MasterId = 1,
                Appearance = _appearanceMock, 
                Details = _detailsMock, 
                ItemCollection = _itemsMock, 
                Statistics = _statisticsMock,
                Familiar = _familiarMock,
                Music = _musicMock,
                Farming = _farmingMock,
                Slayer = _slayerMock,
                Notes = _notesMock,
                Profile = _profileMock,
                ItemAppearanceCollection = _itemAppearanceCollectionMock,
                State = _stateMock
            });

            Assert.IsTrue(await harness.Sent.Any<CharacterDehydrated>());
            Assert.IsTrue(await harness.Consumed.Any<DehydrateCharacter>());

            var consumerHarness = harness.GetConsumerHarness<UpdateCharacterRequestConsumer>();
            var consumerRequest = await consumerHarness.Consumed.SelectAsync<UpdateCharacterRequest>().FirstOrDefault();
            var consumerRequestMessage = consumerRequest?.Context?.Message;

            Assert.IsNotNull(consumerRequestMessage);
            Assert.IsNotNull(consumerRequestMessage.Details);
            Assert.IsNotNull(consumerRequestMessage.ItemCollection);
            Assert.IsNotNull(consumerRequestMessage.Appearance);
            Assert.IsNotNull(consumerRequestMessage.Statistics);

            await harness.Stop();
        }

        [TestMethod]
        public async Task Should_not_dehydrate_notfound_character()
        {
            await using var provider = new ServiceCollection()
            .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<CharacterNotFoundConsumer>();
                x.AddSagaStateMachine<CharacterDehydrationStateMachine, CharacterDehydrationState>();
            })
            .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var client = harness.GetRequestClient<DehydrateCharacter>();
            var response = await client.GetResponse<CharacterDehydrated, CharacterNotFound>(new DehydrateCharacter
            {
                MasterId = 1,
                Appearance = _appearanceMock,
                Details = _detailsMock,
                ItemCollection = _itemsMock,
                Statistics = _statisticsMock,
                Familiar = _familiarMock,
                Music = _musicMock,
                Farming = _farmingMock,
                Slayer = _slayerMock,
                Notes = _notesMock,
                Profile = _profileMock,
                ItemAppearanceCollection = _itemAppearanceCollectionMock,
                State = _stateMock
            });

            Assert.IsTrue(await harness.Sent.Any<CharacterNotFound>());
            Assert.IsTrue(await harness.Consumed.Any<DehydrateCharacter>());

            var consumerHarness = harness.GetConsumerHarness<CharacterNotFoundConsumer>();

            Assert.IsTrue(await consumerHarness.Consumed.Any<UpdateCharacterRequest>());

            await harness.Stop();
        }

        class UpdateCharacterRequestConsumer : IConsumer<UpdateCharacterRequest>
        {
            public async Task Consume(ConsumeContext<UpdateCharacterRequest> context)
            {
                var message = context.Message;
                await context.RespondAsync(new UpdateCharacterResponse(message.CorrelationId, message.MasterId));
            }
        }

        class CharacterNotFoundConsumer : IConsumer<UpdateCharacterRequest>
        {
            public async Task Consume(ConsumeContext<UpdateCharacterRequest> context)
            {
                var message = context.Message;
                await context.RespondAsync(new CharacterNotFound(message.CorrelationId, message.MasterId));
            }
        }
    }
}
