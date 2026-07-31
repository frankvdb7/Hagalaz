using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Consumers;
using Hagalaz.Services.Characters.Data;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.MySql;

namespace Hagalaz.Services.Characters.Tests;

[TestClass]
public sealed class CharacterUpdateRequestConsumerTests
{
    [TestMethod]
    [Timeout(15000)]
    public async Task Consume_PersistCommand_RetriesTransientCommitFailure()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCharacterAsync(databaseName);
        var remainingFailures = 1;
        var totalCommits = 0;

        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateContext(databaseName))
            .AddScoped<ICharacterUnitOfWork>(serviceProvider =>
                new FailingCharacterUnitOfWork(
                    new CharacterUnitOfWork(serviceProvider.GetRequiredService<HagalazDbContext>()),
                    () =>
                    {
                        Interlocked.Increment(ref totalCommits);
                        return Interlocked.Decrement(ref remainingFailures) >= 0;
                    }))
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x =>
                x.AddConsumer<UpdateCharacterRequestConsumer, CharacterPersistenceConsumerDefinition>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var command = CreateCommand();
        await harness.Bus.Publish(command);

        Assert.IsTrue(await harness.Consumed.Any<PersistCharacterCommand>(x => x.Context.Message.MasterId == command.MasterId));
        for (var attempt = 0; attempt < 50 && totalCommits < 2; attempt++)
        {
            await Task.Delay(100);
        }

        await harness.Stop();

        Assert.AreEqual(2, totalCommits);
        await using var verificationContext = CreateContext(databaseName);
        var character = await verificationContext.Characters.SingleAsync(x => x.Id == command.MasterId);
        Assert.AreEqual(command.SnapshotRevision, character.SnapshotRevision);
    }

    [TestMethod]
    public async Task Consume_PersistCommand_IsOneWayAndIgnoresStaleRevision()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCharacterAsync(databaseName);

        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateContext(databaseName))
            .AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>()
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x => x.AddConsumer<UpdateCharacterRequestConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var command = CreateCommand();
        var staleCommand = command with
        {
            SnapshotRevision = 1,
            Details = new DetailsDto(3999, 3998, 1)
        };
        await harness.Bus.Publish(command);
        await harness.Bus.Publish(staleCommand);

        Assert.IsTrue(await harness.Consumed.Any<PersistCharacterCommand>(x => x.Context.Message.MasterId == command.MasterId));
        Assert.IsTrue(await harness.Published.Any<PersistCharacterAcknowledged>(x => x.Context.Message.SnapshotRevision == command.SnapshotRevision));
        Assert.IsTrue(await harness.Published.Any<PersistCharacterAcknowledged>(x => x.Context.Message.SnapshotRevision == staleCommand.SnapshotRevision));
        await harness.Stop();

        await using var verificationContext = CreateContext(databaseName);
        var character = await verificationContext.Characters.SingleAsync(x => x.Id == command.MasterId);
        Assert.AreEqual(command.SnapshotRevision, character.SnapshotRevision);
        Assert.AreEqual(command.Details.CoordX, character.CoordX);
        Assert.AreEqual(command.Details.CoordY, character.CoordY);
    }

    [TestMethod]
    public async Task Consume_ExistingCharacter_PersistsSnapshotBeforeResponding()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCharacterAsync(databaseName);

        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateContext(databaseName))
            .AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>()
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x => x.AddConsumer<UpdateCharacterRequestConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var request = CreateRequest();
        var client = harness.GetRequestClient<UpdateCharacterRequest>();
        var response = await client.GetResponse<UpdateCharacterResponse, CharacterNotFound>(request);

        Assert.IsNotNull(response.Message);
        await harness.Stop();

        await using var verificationContext = CreateContext(databaseName);
        var character = await verificationContext.Characters.SingleAsync(x => x.Id == request.MasterId);
        var statistics = await verificationContext.CharactersStatistics.SingleAsync(x => x.MasterId == request.MasterId);
        var items = await verificationContext.CharactersItems.SingleAsync(x => x.MasterId == request.MasterId);
        var state = await verificationContext.CharactersStates.SingleAsync(x => x.MasterId == request.MasterId);

        Assert.AreEqual(request.Details.CoordX, character.CoordX);
        Assert.AreEqual(request.Details.CoordY, character.CoordY);
        Assert.AreEqual(request.Statistics.AttackLevel, statistics.AttackLevel);
        Assert.AreEqual(request.Statistics.AttackExp, statistics.AttackExp);
        Assert.AreEqual(request.ItemCollection.Bank[0].ItemId, items.ItemId);
        Assert.AreEqual((sbyte)0, items.ContainerType);
        Assert.AreEqual(request.State.StatesEx[0].Id.ToString(), state.StateId);
        Assert.AreEqual(request.Profile.JsonData, (await verificationContext.CharacterProfiles.SingleAsync(x => x.MasterId == request.MasterId)).Data);
        Assert.AreEqual(request.Music.UnlockedMusicIds[0].ToString(), (await verificationContext.CharactersMusics.SingleAsync(x => x.MasterId == request.MasterId)).UnlockedMusic.Split(',')[0]);
    }

    [TestMethod]
    public async Task Consume_StaleSnapshot_DoesNotReplaceNewerChildCollections()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCharacterAsync(databaseName);

        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateContext(databaseName))
            .AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>()
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x => x.AddConsumer<UpdateCharacterRequestConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();
        var client = harness.GetRequestClient<UpdateCharacterRequest>();

        var currentRequest = CreateRequest();
        await client.GetResponse<UpdateCharacterResponse>(currentRequest);

        var staleRequest = CreateRequest() with
        {
            SnapshotRevision = 1,
            Details = new DetailsDto(3999, 3998, 1),
            ItemCollection = new ItemCollectionDto
            {
                Bank = [new ItemDto(999, 1, 2, "bank")], Inventory = [], FamiliarInventory = [], Equipment = [], Rewards = [], MoneyPouch = []
            },
            Farming = new FarmingDto { Patches = [new FarmingDto.PatchDto { Id = 999, SeedId = 999 }] },
            Notes = new NotesDto { Notes = [new NotesDto.NoteDto { Id = 999, Text = "stale" }] },
            ItemAppearanceCollection = new ItemAppearanceCollectionDto { Appearances = [new ItemAppearanceDto { Id = 999 }] },
            State = new StateDto { StatesEx = [new StateDto.StateExDto { Id = 999, TicksLeft = 999 }] }
        };
        var response = await client.GetResponse<UpdateCharacterResponse>(staleRequest);

        Assert.AreEqual(staleRequest.MasterId, response.Message.MasterId);
        await harness.Stop();

        await using var verificationContext = CreateContext(databaseName);
        var character = await verificationContext.Characters.SingleAsync(x => x.Id == currentRequest.MasterId);
        var item = await verificationContext.CharactersItems.SingleAsync(x => x.MasterId == currentRequest.MasterId);
        var farmingPatch = await verificationContext.CharactersFarmingPatches.SingleAsync(x => x.MasterId == currentRequest.MasterId);
        var note = await verificationContext.CharactersNotes.SingleAsync(x => x.MasterId == currentRequest.MasterId);
        var itemAppearance = await verificationContext.CharactersItemsLooks.SingleAsync(x => x.MasterId == currentRequest.MasterId);
        var state = await verificationContext.CharactersStates.SingleAsync(x => x.MasterId == currentRequest.MasterId);

        Assert.AreEqual(currentRequest.SnapshotRevision, character.SnapshotRevision);
        Assert.AreEqual(currentRequest.Details.CoordX, character.CoordX);
        Assert.AreEqual(currentRequest.ItemCollection.Bank[0].ItemId, item.ItemId);
        Assert.AreEqual((uint)currentRequest.Farming.Patches[0].Id, farmingPatch.PatchId);
        Assert.AreEqual(currentRequest.Notes.Notes[0].Id, note.NoteId);
        Assert.AreEqual(currentRequest.ItemAppearanceCollection.Appearances[0].Id, itemAppearance.ItemId);
        Assert.AreEqual(currentRequest.State.StatesEx[0].Id.ToString(), state.StateId);
    }

    [TestMethod]
    public async Task Consume_UnknownCharacter_RespondsCharacterNotFound()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateContext(databaseName))
            .AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>()
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x => x.AddConsumer<UpdateCharacterRequestConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var request = CreateRequest() with { MasterId = 404 };
        var client = harness.GetRequestClient<UpdateCharacterRequest>();
        var response = await client.GetResponse<CharacterNotFound>(request);

        Assert.AreEqual(request.MasterId, response.Message.MasterId);
        await harness.Stop();
    }

    [TestMethod]
    public async Task Consume_InactiveSlayerSentinel_RemovesExistingTaskAndCommits()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCharacterAsync(databaseName);
        await using (var seedContext = CreateContext(databaseName))
        {
            seedContext.CharactersSlayerTasks.Add(new CharactersSlayerTask { MasterId = 1, SlayerTaskId = 30, KillCount = 10 });
            await seedContext.SaveChangesAsync();
        }

        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateContext(databaseName))
            .AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>()
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x => x.AddConsumer<UpdateCharacterRequestConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var request = CreateRequest() with
        {
            Slayer = new SlayerDto { Task = new SlayerDto.SlayerTaskDto { Id = -1, KillCount = 0 } }
        };
        var client = harness.GetRequestClient<UpdateCharacterRequest>();
        var response = await client.GetResponse<UpdateCharacterResponse>(request);

        Assert.AreEqual(request.MasterId, response.Message.MasterId);
        await harness.Stop();

        await using var verificationContext = CreateContext(databaseName);
        Assert.IsFalse(await verificationContext.CharactersSlayerTasks.AnyAsync(x => x.MasterId == request.MasterId));
    }

    [TestMethod]
    [Timeout(120_000)]
    public async Task Consume_DefaultFamiliarSentinel_DoesNotPersistFamiliar()
    {
        await using var database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-characters-test")
            .WithUsername("root")
            .WithPassword("hagalaz-characters-test")
            .WithCommand(
                "--character-set-server=utf8mb4",
                "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await database.StartAsync();

        await using (var seedContext = CreateMySqlContext(database))
        {
            await seedContext.Database.MigrateAsync();
            seedContext.Characters.Add(new Character
            {
                Id = 1,
                UserName = "test-character",
                NormalizedUserName = "TEST-CHARACTER",
                DisplayName = "Test",
                RegisterIp = "127.0.0.1"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var provider = new ServiceCollection()
            .AddScoped(_ => CreateMySqlContext(database))
            .AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>()
            .AddAutoMapper(_ => { }, typeof(Program))
            .AddMassTransitTestHarness(x => x.AddConsumer<UpdateCharacterRequestConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var request = CreateRequest() with
        {
            Familiar = new FamiliarDto(0, 0, false, 0),
            Farming = new FarmingDto { Patches = [] },
            Slayer = new SlayerDto()
        };
        var client = harness.GetRequestClient<UpdateCharacterRequest>();
        var response = await client.GetResponse<UpdateCharacterResponse>(request);

        Assert.AreEqual(request.MasterId, response.Message.MasterId);
        await harness.Stop();

        await using var verificationContext = CreateMySqlContext(database);
        Assert.IsFalse(await verificationContext.CharactersFamiliars.AnyAsync(x => x.MasterId == request.MasterId));
        Assert.IsFalse(await verificationContext.CharactersFamiliars.AnyAsync(x => x.FamiliarId == 0));
    }

    [TestMethod]
    public async Task Consume_WhenCommitFails_PropagatesFailureWithoutSuccessResponse()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCharacterAsync(databaseName);
        await using var context = CreateContext(databaseName);
        var unitOfWork = new FailingCharacterUnitOfWork(new CharacterUnitOfWork(context));
        var mapper = CreateMapper();
        var consumer = new UpdateCharacterRequestConsumer(unitOfWork, mapper);
        var consumeContext = new Mock<ConsumeContext<UpdateCharacterRequest>>();
        consumeContext.SetupGet(x => x.Message).Returns(CreateRequest());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(consumeContext.Object));

        Assert.AreEqual("The character update could not be committed.", exception.Message);
        Assert.IsFalse(consumeContext.Invocations.Any(invocation => invocation.Method.Name == "RespondAsync"));
    }

    [TestMethod]
    public async Task Consume_NonPositiveSnapshotRevision_RejectsRequest()
    {
        var consumer = new UpdateCharacterRequestConsumer(new Mock<ICharacterUnitOfWork>().Object, CreateMapper());
        var consumeContext = new Mock<ConsumeContext<UpdateCharacterRequest>>();
        consumeContext.SetupGet(x => x.Message).Returns(CreateRequest() with { SnapshotRevision = 0 });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => consumer.Consume(consumeContext.Object));

        Assert.IsFalse(consumeContext.Invocations.Any(invocation => invocation.Method.Name == "RespondAsync"));
    }

    [TestMethod]
    public void SnapshotRevision_IsConfiguredAsAnOptimisticConcurrencyToken()
    {
        using var context = CreateContext(Guid.NewGuid().ToString());
        var property = context.Model.FindEntityType(typeof(Character))!.FindProperty(nameof(Character.SnapshotRevision));

        Assert.IsNotNull(property);
        Assert.IsTrue(property.IsConcurrencyToken);
    }

    private static UpdateCharacterRequest CreateRequest() => new(
        Guid.NewGuid(),
        1,
        new AppearanceDto(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14),
        new DetailsDto(3200, 3201, 2),
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
            Bank = [new ItemDto(100, 5, 2, "bank")], Inventory = [], FamiliarInventory = [], Equipment = [], Rewards = [], MoneyPouch = []
        },
        new FamiliarDto(1, 2, true, 3),
        new MusicDto([10, 11], [12], true, false),
        new FarmingDto { Patches = [new FarmingDto.PatchDto { Id = 20, SeedId = 21, Condition = 22, CurrentCycle = 23, CurrentCycleTicks = 24, ProductCount = 25 }] },
        new SlayerDto { Task = new SlayerDto.SlayerTaskDto { Id = 30, KillCount = 31 } },
        new NotesDto { Notes = [new NotesDto.NoteDto { Id = 32, Color = 33, Text = "note" }] },
        new ProfileDto { JsonData = "{\"changed\":true}" },
        new ItemAppearanceCollectionDto { Appearances = [new ItemAppearanceDto { Id = 40, MaleModels = [1, 2, 3], FemaleModels = [4, 5, 6], ModelColors = [7, 8], TextureColors = [9, 10] }] },
        new StateDto { StatesEx = [new StateDto.StateExDto { Id = 50, TicksLeft = 51 }] },
        2);

    private static PersistCharacterCommand CreateCommand()
    {
        var request = CreateRequest();
        return new PersistCharacterCommand(
            request.CorrelationId,
            request.MasterId,
            request.Appearance,
            request.Details,
            request.Statistics,
            request.ItemCollection,
            request.Familiar,
            request.Music,
            request.Farming,
            request.Slayer,
            request.Notes,
            request.Profile,
            request.ItemAppearanceCollection,
            request.State,
            request.SnapshotRevision);
    }

    private static async Task SeedCharacterAsync(string databaseName)
    {
        await using var context = CreateContext(databaseName);
        context.Characters.Add(new Character
        {
            Id = 1,
            UserName = "test-character",
            NormalizedUserName = "TEST-CHARACTER",
            DisplayName = "Test Character",
            RegisterIp = "127.0.0.1"
        });
        context.CharactersFarmingPatches.Add(new CharactersFarmingPatch { MasterId = 1, PatchId = 20, SeedId = 1 });
        context.CharactersNotes.Add(new CharactersNote { MasterId = 1, NoteId = 32, Text = "old note" });
        context.CharactersItemsLooks.Add(new CharactersItemsLook { MasterId = 1, ItemId = 40 });
        context.CharactersStates.Add(new CharactersState { MasterId = 1, StateId = "50", TicksLeft = 1 });
        await context.SaveChangesAsync();
    }

    private static HagalazDbContext CreateContext(string databaseName) => new(
        new DbContextOptionsBuilder<HagalazDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);

    private static HagalazDbContext CreateMySqlContext(MySqlContainer database) => new(
        new DbContextOptionsBuilder<HagalazDbContext>()
            .UseMySQL(database.GetConnectionString())
            .Options);

    private static IMapper CreateMapper()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(_ => { }, typeof(Program))
            .BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private sealed class FailingCharacterUnitOfWork : ICharacterUnitOfWork
    {
        private readonly ICharacterUnitOfWork _inner;
        private readonly Func<bool> _shouldFail;

        public FailingCharacterUnitOfWork(ICharacterUnitOfWork inner)
            : this(inner, static () => true)
        {
        }

        public FailingCharacterUnitOfWork(ICharacterUnitOfWork inner, Func<bool> shouldFail)
        {
            _inner = inner;
            _shouldFail = shouldFail;
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
        public ValueTask RollbackAsync() => _inner.RollbackAsync();
        public ValueTask CommitAsync()
        {
            if (_shouldFail())
            {
                return ValueTask.FromException(new InvalidOperationException("The character update could not be committed."));
            }

            return _inner.CommitAsync();
        }
    }
}
