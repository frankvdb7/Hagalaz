using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Consumers;
using Hagalaz.Services.Characters.Data;
using Hagalaz.Services.Characters.Metrics;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MySql;

namespace Hagalaz.Services.Characters.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CharacterPersistenceIntegrationTests
{
    private static MySqlContainer? _database;
    private static int _nextMasterId = 1000;

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-characters-integration-test")
            .WithUsername("root")
            .WithPassword("hagalaz-characters-integration-test")
            .WithCommand(
                "--character-set-server=utf8mb4",
                "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await _database.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    [ClassCleanup]
    public static async Task Cleanup()
    {
        if (_database != null)
        {
            await _database.DisposeAsync();
        }
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task ProductionEfBusOutbox_PersistsCommandSnapshotAndAcknowledgement()
    {
        var masterId = await SeedCharacterAsync();
        await using var provider = CreateProvider();
        var harness = provider.GetTestHarness();
        var acknowledgementProbe = harness.GetConsumerHarness<AcknowledgementProbe>();
        await harness.Start();

        try
        {
            var command = CreateCommand(masterId, 1);
            using (var scope = provider.CreateScope())
            {
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                await publishEndpoint.Publish(command);
                await scope.ServiceProvider.GetRequiredService<HagalazDbContext>().SaveChangesAsync();
            }

            Assert.IsTrue(await harness.Consumed.Any<PersistCharacterCommand>(x =>
                x.Context.Message.MasterId == masterId && x.Context.Message.SnapshotRevision == 1));
            Assert.IsTrue(await acknowledgementProbe.Consumed.Any<PersistCharacterAcknowledged>(x =>
                x.Context.Message.MasterId == masterId && x.Context.Message.SnapshotRevision == 1));
        }
        finally
        {
            await harness.Stop();
        }

        await using var verificationContext = CreateContext();
        var character = await verificationContext.Characters.SingleAsync(x => x.Id == masterId);
        Assert.AreEqual(1L, character.SnapshotRevision);
        Assert.AreEqual(3200, character.CoordX);
    }

    [TestMethod]
    [Timeout(45000)]
    public async Task ConcurrentCommands_RetryAgainstFreshEfStateAndApplyHighestRevision()
    {
        var masterId = await SeedCharacterAsync();
        var barrier = new CommitBarrier(2);
        await using var provider = CreateProvider(barrier);
        var harness = provider.GetTestHarness();
        var acknowledgementProbe = harness.GetConsumerHarness<AcknowledgementProbe>();
        await harness.Start();

        try
        {
            var olderCommand = CreateCommand(masterId, 2, 3202);
            var newerCommand = CreateCommand(masterId, 3, 3203);
            await Task.WhenAll(harness.Bus.Publish(olderCommand), harness.Bus.Publish(newerCommand));

            Assert.IsTrue(await acknowledgementProbe.Consumed.Any<PersistCharacterAcknowledged>(x =>
                x.Context.Message.MasterId == masterId && x.Context.Message.SnapshotRevision == 2));
            Assert.IsTrue(await acknowledgementProbe.Consumed.Any<PersistCharacterAcknowledged>(x =>
                x.Context.Message.MasterId == masterId && x.Context.Message.SnapshotRevision == 3));
        }
        finally
        {
            await harness.Stop();
        }

        await using var verificationContext = CreateContext();
        var character = await verificationContext.Characters.SingleAsync(x => x.Id == masterId);
        Assert.AreEqual(3L, character.SnapshotRevision);
        Assert.AreEqual(3203, character.CoordX);
    }

    [TestMethod]
    [Timeout(150000)]
    public async Task FailedDatabaseCommit_RollsBackAcknowledgementAndReachesErrorAndFaultPaths()
    {
        var masterId = await SeedCharacterAsync();
        await using var provider = CreateProvider(fastFailure: true);
        var harness = provider.GetTestHarness();
        var faultConsumer = harness.GetConsumerHarness<CharacterPersistenceFaultConsumer>();
        var errorQueue = harness.GetConsumerHarness<ErrorQueueProbe>();
        var acknowledgementProbe = harness.GetConsumerHarness<AcknowledgementProbe>();
        await harness.Start();

        try
        {
            var command = CreateCommand(masterId, 1, noteText: new string('x', 51));
            await harness.Bus.Publish(command);

            using var consumedTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Assert.IsTrue(await harness.Consumed.Any<PersistCharacterCommand>(x =>
                x.Context.Message.MasterId == masterId,
                consumedTimeout.Token));
            Assert.IsTrue(await errorQueue.Consumed.Any<PersistCharacterCommand>(x =>
                x.Context.Message.MasterId == masterId,
                consumedTimeout.Token));
            using var acknowledgementTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Assert.IsFalse(await acknowledgementProbe.Consumed.Any<PersistCharacterAcknowledged>(x =>
                x.Context.Message.MasterId == masterId,
                acknowledgementTimeout.Token));
            await harness.Bus.Publish<Fault<PersistCharacterCommand>>(new
            {
                Message = command
            });
            using var faultConsumerTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Assert.IsTrue(await faultConsumer.Consumed.Any<Fault<PersistCharacterCommand>>(x =>
                x.Context.Message.Message.MasterId == masterId,
                faultConsumerTimeout.Token));
        }
        finally
        {
            await harness.Stop();
        }

        await using var verificationContext = CreateContext();
        var character = await verificationContext.Characters.SingleAsync(x => x.Id == masterId);
        var note = await verificationContext.CharactersNotes.SingleAsync(x => x.MasterId == masterId);
        Assert.AreEqual(0L, character.SnapshotRevision);
        Assert.AreEqual("old note", note.Text);
    }

    private static ServiceProvider CreateProvider(
        CommitBarrier? barrier = null,
        bool fastFailure = false)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<CharacterPersistenceMetrics>()
            .AddDbContext<HagalazDbContext>(options => options.UseMySQL(_database!.GetConnectionString()))
            .AddScoped<ICharacterUnitOfWork>(serviceProvider =>
            {
                var unitOfWork = new CharacterUnitOfWork(serviceProvider.GetRequiredService<HagalazDbContext>());
                return barrier == null
                    ? unitOfWork
                    : new BarrierCharacterUnitOfWork(unitOfWork, barrier);
            })
            .AddAutoMapper(_ => { }, typeof(Program));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddEntityFrameworkOutbox<HagalazDbContext>(options =>
            {
                options.UseMySql();
                options.UseBusOutbox();
            });
            if (fastFailure)
            {
                x.AddConsumer<UpdateCharacterRequestConsumer, FastCharacterPersistenceConsumerDefinition>();
            }
            else
            {
                x.AddConsumer<UpdateCharacterRequestConsumer, CharacterPersistenceConsumerDefinition>();
            }
            x.AddConsumer<CharacterPersistenceFaultConsumer>();
            x.AddConsumer<AcknowledgementProbe>();
            if (fastFailure)
            {
                x.AddConsumer<ErrorQueueProbe, ErrorQueueProbeDefinition>();
            }
            x.AddConfigureEndpointsCallback((name, endpoint) =>
            {
                if (name == "UpdateCharacterRequest")
                {
                    endpoint.ConcurrentMessageLimit = 2;
                    endpoint.PublishFaults = true;
                }
            });
        });

        return services.BuildServiceProvider(true);
    }

    private static async Task<uint> SeedCharacterAsync()
    {
        var masterId = (uint)Interlocked.Increment(ref _nextMasterId);
        await using var context = CreateContext();
        context.Characters.Add(new Character
        {
            Id = masterId,
            UserName = $"integration-{masterId}",
            NormalizedUserName = $"INTEGRATION-{masterId}",
            DisplayName = $"Test{masterId}",
            RegisterIp = "127.0.0.1"
        });
        context.CharactersLooks.Add(new CharactersLook { MasterId = masterId });
        context.CharactersStatistics.Add(new CharactersStatistic { MasterId = masterId });
        context.CharactersMusics.Add(new CharactersMusic { MasterId = masterId, UnlockedMusic = "" });
        context.CharactersMusicPlaylists.Add(new CharactersMusicPlaylist { MasterId = masterId, Playlist = "" });
        context.CharacterProfiles.Add(new CharactersProfile { MasterId = masterId, Data = "{}" });
        context.CharactersNotes.Add(new CharactersNote { MasterId = masterId, NoteId = 32, Text = "old note" });
        context.CharactersItemsLooks.Add(new CharactersItemsLook { MasterId = masterId, ItemId = 40 });
        context.CharactersStates.Add(new CharactersState { MasterId = masterId, StateId = "50", TicksLeft = 1 });
        await context.SaveChangesAsync();
        return masterId;
    }

    private static HagalazDbContext CreateContext() => new(
        new DbContextOptionsBuilder<HagalazDbContext>()
            .UseMySQL(_database!.GetConnectionString())
            .Options);

    private static PersistCharacterCommand CreateCommand(uint masterId, long revision, int coordX = 3200, string noteText = "note") =>
        new(
            Guid.NewGuid(),
            masterId,
            new AppearanceDto(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14),
            new DetailsDto(coordX, 3201, 2),
            new StatisticsDto(
                AttackLevel: 42, AttackExp: 12345, DefenceLevel: 3, DefenceExp: 4, StrengthLevel: 5, StrengthExp: 6,
                ConstitutionLevel: 7, ConstitutionExp: 8, RangeLevel: 9, RangeExp: 10, PrayerLevel: 11, PrayerExp: 12,
                MagicLevel: 13, MagicExp: 14, CookingLevel: 15, CookingExp: 16, WoodcuttingLevel: 17, WoodcuttingExp: 18,
                FletchingLevel: 19, FletchingExp: 20, FishingLevel: 21, FishingExp: 22, FiremakingLevel: 23, FiremakingExp: 24,
                CraftingLevel: 25, CraftingExp: 26, SmithingLevel: 27, SmithingExp: 28, MiningLevel: 29, MiningExp: 30,
                HerbloreLevel: 31, HerbloreExp: 32, AgilityLevel: 33, AgilityExp: 34, ThievingLevel: 35, ThievingExp: 36,
                SlayerLevel: 37, SlayerExp: 38, FarmingLevel: 39, FarmingExp: 40, RunecraftingLevel: 41, RunecraftingExp: 42,
                ConstructionLevel: 43, ConstructionExp: 44, HunterLevel: 45, HunterExp: 46, SummoningLevel: 47, SummoningExp: 48,
                DungeoneeringLevel: 49, DungeoneeringExp: 50, LifePoints: 51, PrayerPoints: 52, RunEnergy: 53,
                SpecialEnergy: 54, PoisonAmount: 55, PlayTime: 56, XpCounters: [1, 2], TrackedXpCounters: [3, 4],
                EnabledXpCounters: [true, false], TargetSkillLevels: [5, 6], TargetSkillExperiences: [7.5, 8.5]),
            new ItemCollectionDto
            {
                Bank = [], Inventory = [], FamiliarInventory = [], Equipment = [], Rewards = [], MoneyPouch = []
            },
            new FamiliarDto(0, 0, false, 0),
            new MusicDto([10, 11], [12], true, false),
            new FarmingDto { Patches = [] },
            new SlayerDto { Task = new SlayerDto.SlayerTaskDto { Id = -1, KillCount = 0 } },
            new NotesDto { Notes = [new NotesDto.NoteDto { Id = 32, Color = 33, Text = noteText }] },
            new ProfileDto { JsonData = "{\"changed\":true}" },
            new ItemAppearanceCollectionDto { Appearances = [new ItemAppearanceDto { Id = 40, MaleModels = [1, 2, 3], FemaleModels = [4, 5, 6], ModelColors = [7, 8], TextureColors = [9, 10] }] },
            new StateDto { StatesEx = [new StateDto.StateExDto { Id = 50, TicksLeft = 51 }] },
            revision);

    private sealed class AcknowledgementProbe : IConsumer<PersistCharacterAcknowledged>
    {
        public Task Consume(ConsumeContext<PersistCharacterAcknowledged> context) => Task.CompletedTask;
    }

    private sealed class ErrorQueueProbe : IConsumer<PersistCharacterCommand>
    {
        public Task Consume(ConsumeContext<PersistCharacterCommand> context) => Task.CompletedTask;
    }

    private sealed class ErrorQueueProbeDefinition : ConsumerDefinition<ErrorQueueProbe>
    {
        public ErrorQueueProbeDefinition() => EndpointName = "UpdateCharacterRequest_error";

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ErrorQueueProbe> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.ConfigureConsumeTopology = false;
        }
    }

    private sealed class CommitBarrier
    {
        private readonly int _requiredArrivals;
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;

        public CommitBarrier(int requiredArrivals) => _requiredArrivals = requiredArrivals;

        public async ValueTask WaitAsync()
        {
            if (Interlocked.Increment(ref _arrivals) >= _requiredArrivals)
            {
                _released.TrySetResult();
            }

            await _released.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    private sealed class FastCharacterPersistenceConsumerDefinition : ConsumerDefinition<UpdateCharacterRequestConsumer>
    {
        public FastCharacterPersistenceConsumerDefinition() => EndpointName = "UpdateCharacterRequest";

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<UpdateCharacterRequestConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.ConfigureDefaultErrorTransport();
            endpointConfigurator.ConfigureDefaultDeadLetterTransport();
            endpointConfigurator.UseEntityFrameworkOutbox<HagalazDbContext>(context);
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(1));
        }
    }

    private sealed class BarrierCharacterUnitOfWork : ICharacterUnitOfWork
    {
        private readonly ICharacterUnitOfWork _inner;
        private readonly CommitBarrier _barrier;

        public BarrierCharacterUnitOfWork(ICharacterUnitOfWork inner, CommitBarrier barrier)
        {
            _inner = inner;
            _barrier = barrier;
        }

        public ICharacterRepository CharacterRepository => _inner.CharacterRepository;
        public ICharacterStatisticsRepository CharacterStatisticsRepository => _inner.CharacterStatisticsRepository;
        public ICharacterItemRepository CharacterItemRepository => _inner.CharacterItemRepository;
        public ICharacterLookRepository CharacterLookRepository => _inner.CharacterLookRepository;
        public ICharacterItemLookRepository CharacterItemLookRepository => _inner.CharacterItemLookRepository;
        public ICharacterFamiliarRepository CharacterFamiliarRepository => _inner.CharacterFamiliarRepository;
        public ICharacterMusicRepository CharacterMusicRepository => _inner.CharacterMusicRepository;
        public ICharacterMusicPlaylistRepository CharacterMusicPlaylistRepository => _inner.CharacterMusicPlaylistRepository;
        public ICharacterFarmingRepository CharacterFarmingRepository => _inner.CharacterFarmingRepository;
        public ICharacterSlayerRepository CharacterSlayerRepository => _inner.CharacterSlayerRepository;
        public ICharacterNotesRepository CharacterNotesRepository => _inner.CharacterNotesRepository;
        public ICharacterProfileRepository CharacterProfileRepository => _inner.CharacterProfileRepository;
        public ICharacterStateRepository CharacterStateRepository => _inner.CharacterStateRepository;

        public void Add<TEntity>(TEntity entity) where TEntity : class => _inner.Add(entity);
        public void Remove<TEntity>(TEntity entity) where TEntity : class => _inner.Remove(entity);
        public void Reset() => _inner.Reset();
        public ValueTask RollbackAsync() => _inner.RollbackAsync();

        public async ValueTask CommitAsync()
        {
            await _barrier.WaitAsync();
            await _inner.CommitAsync();
        }
    }

}
