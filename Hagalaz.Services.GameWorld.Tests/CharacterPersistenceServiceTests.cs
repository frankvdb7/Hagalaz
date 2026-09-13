using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Data;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterPersistenceServiceTests
{
    [TestMethod]
    public async Task PersistAsync_SkipsUnacknowledgedSnapshotAndPreservesFingerprintDeduplication()
    {
        var snapshot = new CharacterModel();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var publishedCommands = new System.Collections.Generic.List<PersistCharacterCommand>();
        publishEndpoint
            .When(endpoint => endpoint.Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => publishedCommands.Add(callInfo.Arg<PersistCharacterCommand>()!));

        await using var dbContext = CreateSharedDbContext();
        using var mapperProvider = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(configuration => configuration.AddProfile<CharacterProfile>())
            .BuildServiceProvider();
        var mapper = mapperProvider.GetRequiredService<AutoMapper.IMapper>();
        var character = Substitute.For<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>();
        character.MasterId.Returns(42u);
        var state = new CharacterPersistenceState();
        var service = new CharacterPersistenceService(
            NullLogger<CharacterPersistenceService>.Instance,
            mapper,
            publishEndpoint,
            dbContext,
            state);

        service.InitializeRevision(42, 100);

        var firstReceipt = await service.PersistAsync(42, snapshot, force: false);
        var skippedReceipt = await service.PersistAsync(42, snapshot, force: false);

        Assert.IsNotNull(firstReceipt);
        Assert.IsNull(skippedReceipt);
        Assert.HasCount(1, publishedCommands);
        Assert.AreEqual(42u, publishedCommands[0].MasterId);
        Assert.AreEqual(101L, publishedCommands[0].SnapshotRevision);

        state.Acknowledge(42, publishedCommands[0].CorrelationId, publishedCommands[0].SnapshotRevision, CharacterPersistenceOutcome.Committed);
        await service.PersistAsync(42, snapshot, force: false);

        await publishEndpoint.Received(1).Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task PersistAsync_UsesNewRevisionWhenForced()
    {
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        await using var dbContext = CreateSharedDbContext();
        using var mapperProvider = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(configuration => configuration.AddProfile<CharacterProfile>())
            .BuildServiceProvider();
        var mapper = mapperProvider.GetRequiredService<AutoMapper.IMapper>();
        var character = Substitute.For<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>();
        character.MasterId.Returns(42u);
        var state = new CharacterPersistenceState();
        var service = new CharacterPersistenceService(
            NullLogger<CharacterPersistenceService>.Instance,
            mapper,
            publishEndpoint,
            dbContext,
            state);

        var firstReceipt = await service.PersistAsync(42, new CharacterModel(), force: true);
        state.Acknowledge(42, firstReceipt!.CorrelationId, firstReceipt.SnapshotRevision, CharacterPersistenceOutcome.Committed);
        var secondReceipt = await service.PersistAsync(42, new CharacterModel(), force: true);

        Assert.AreNotEqual(firstReceipt.SnapshotRevision, secondReceipt!.SnapshotRevision);
        await publishEndpoint.Received(2).Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task PersistAsync_AfterConflictCreatesNewReceiptAndRevision()
    {
        await using var harness = new PersistenceHarness();
        var firstReceipt = await harness.Service.PersistAsync(42, harness.CurrentModel, force: false);

        harness.State.Acknowledge(
            42,
            firstReceipt!.CorrelationId,
            firstReceipt.SnapshotRevision,
            CharacterPersistenceOutcome.Conflict);

        var retry = await harness.Service.PersistAsync(42, harness.CurrentModel, force: false);

        Assert.IsNotNull(retry);
        Assert.AreNotEqual(firstReceipt.CorrelationId, retry.CorrelationId);
        Assert.IsTrue(retry.SnapshotRevision > firstReceipt.SnapshotRevision);
    }

    [TestMethod]
    public async Task PersistAsync_ForcedSaveWaitsForPendingAndUsesCurrentCharacterState()
    {
        await using var harness = new PersistenceHarness();
        var firstReceipt = await harness.Service.PersistAsync(42, harness.CurrentModel, force: false);
        var forcedModel = new CharacterModel
        {
            Details = new Hagalaz.Services.GameWorld.Logic.Characters.Model.HydratedDetailsDto
            {
                CoordX = 99,
                CoordY = 2,
                CoordZ = 3
            }
        };
        var forcedSave = harness.Service.PersistAsync(42, forcedModel, force: true);

        Assert.IsFalse(forcedSave.IsCompleted);
        harness.State.Acknowledge(
            42,
            firstReceipt!.CorrelationId,
            firstReceipt.SnapshotRevision,
            CharacterPersistenceOutcome.Committed);

        var forcedReceipt = await forcedSave;

        Assert.IsNotNull(forcedReceipt);
        Assert.HasCount(2, harness.PublishedCommands);
        Assert.AreEqual(99, harness.PublishedCommands[1].Details.CoordX);
    }

    [TestMethod]
    public async Task PersistAsync_OlderAcknowledgementCannotCompleteNewerForcedReceipt()
    {
        await using var harness = new PersistenceHarness();
        var firstReceipt = await harness.Service.PersistAsync(42, harness.CurrentModel, force: false);
        var forcedSave = harness.Service.PersistAsync(42, harness.CurrentModel, force: true);

        harness.State.Acknowledge(
            42,
            firstReceipt!.CorrelationId,
            firstReceipt.SnapshotRevision,
            CharacterPersistenceOutcome.Committed);
        var forcedReceipt = await forcedSave;
        var forcedCompletion = harness.Service.WaitForAcknowledgementAsync(forcedReceipt!);

        harness.State.Acknowledge(
            42,
            firstReceipt.CorrelationId,
            firstReceipt.SnapshotRevision,
            CharacterPersistenceOutcome.Committed);

        Assert.IsFalse(forcedCompletion.IsCompleted);

        harness.State.Acknowledge(
            42,
            forcedReceipt!.CorrelationId,
            forcedReceipt.SnapshotRevision,
            CharacterPersistenceOutcome.Committed);
        Assert.AreEqual(CharacterPersistenceOutcome.Committed, await forcedCompletion);
    }

    [TestMethod]
    public async Task PersistAsync_ConcurrentPeriodicAttemptsCreateOnePendingSnapshot()
    {
        await using var harness = new PersistenceHarness();
        var receipts = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => harness.Service.PersistAsync(42, harness.CurrentModel, force: false)));

        Assert.AreEqual(1, receipts.Count(receipt => receipt is not null));
        Assert.HasCount(1, harness.PublishedCommands);
    }

    [TestMethod]
    public async Task PersistAsync_CancellationWhileWaitingPreservesTheExistingOwner()
    {
        await using var harness = new PersistenceHarness();
        var firstReceipt = await harness.Service.PersistAsync(42, harness.CurrentModel, force: false);
        using var cancellation = new CancellationTokenSource();
        var forcedSave = harness.Service.PersistAsync(42, harness.CurrentModel, force: true, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => forcedSave);
        Assert.IsNull(await harness.Service.PersistAsync(42, harness.CurrentModel, force: false));

        harness.State.Acknowledge(
            42,
            firstReceipt!.CorrelationId,
            firstReceipt.SnapshotRevision,
            CharacterPersistenceOutcome.Committed);
    }

    [TestMethod]
    public async Task PersistAsync_PublishFailure_ReleasesPendingOwnershipAndConsumesRevision()
    {
        await using var harness = new PersistenceHarness();
        var publishFailure = new InvalidOperationException("publish failed");
        harness.PublishException = publishFailure;

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => harness.Service.PersistAsync(42, harness.CurrentModel, force: false));

        Assert.AreSame(publishFailure, exception);
        harness.PublishException = null;

        var retry = await harness.Service.PersistAsync(42, harness.CurrentModel, force: false);

        Assert.IsNotNull(retry);
        Assert.HasCount(2, harness.PublishedCommands);
        Assert.IsTrue(harness.PublishedCommands[1].SnapshotRevision > harness.PublishedCommands[0].SnapshotRevision);
    }

    [TestMethod]
    public async Task PersistAsync_SaveChangesFailure_ReleasesPendingOwnershipAndConsumesRevision()
    {
        await using var harness = new PersistenceHarness();
        var saveFailure = new InvalidOperationException("outbox commit failed");
        harness.SaveChangesException = saveFailure;

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => harness.Service.PersistAsync(42, harness.CurrentModel, force: false));

        Assert.AreSame(saveFailure, exception);
        harness.SaveChangesException = null;

        var retry = await harness.Service.PersistAsync(42, harness.CurrentModel, force: false);

        Assert.IsNotNull(retry);
        Assert.HasCount(2, harness.PublishedCommands);
        Assert.IsTrue(harness.PublishedCommands[1].SnapshotRevision > harness.PublishedCommands[0].SnapshotRevision);
    }

    [TestMethod]
    public void SharedDbContext_ContainsMassTransitInboxAndOutboxEntities()
    {
        using var context = CreateSharedDbContext();
        var tableNames = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("InboxState", tableNames);
        Assert.Contains("OutboxMessage", tableNames);
        Assert.Contains("OutboxState", tableNames);
    }

    private static HagalazDbContext CreateSharedDbContext()
    {
        var options = new DbContextOptionsBuilder<HagalazDbContext>()
            .UseMySQL("Server=localhost;Database=hagalaz;User=root;Password=;")
            .Options;
        return new HagalazDbContext(options);
    }

    private sealed class PersistenceHarness : IAsyncDisposable
    {
        private readonly HagalazDbContext _dbContext;
        private readonly ServiceProvider _mapperProvider;

        public PersistenceHarness()
        {
            Character = Substitute.For<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>();
            Character.MasterId.Returns(42u);
            State = new CharacterPersistenceState();
            CurrentModel = new CharacterModel();
            PublishEndpoint = Substitute.For<IPublishEndpoint>();
            PublishEndpoint
                .When(endpoint => endpoint.Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>()))
                .Do(callInfo =>
                {
                    PublishedCommands.Add(callInfo.Arg<PersistCharacterCommand>()!);
                    if (PublishException is not null)
                    {
                        throw PublishException;
                    }
                });
            _dbContext = Substitute.For<HagalazDbContext>(CreateDbContextOptions());
            _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(_ => SaveChangesException is null
                    ? Task.FromResult(1)
                    : Task.FromException<int>(SaveChangesException));
            _mapperProvider = new ServiceCollection()
                .AddLogging()
                .AddAutoMapper(configuration => configuration.AddProfile<CharacterProfile>())
                .BuildServiceProvider();

            Service = new CharacterPersistenceService(
                NullLogger<CharacterPersistenceService>.Instance,
                _mapperProvider.GetRequiredService<AutoMapper.IMapper>(),
                PublishEndpoint,
                _dbContext,
                State);
        }

        public Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter Character { get; }
        public CharacterPersistenceState State { get; }
        public CharacterModel CurrentModel { get; set; }
        public IPublishEndpoint PublishEndpoint { get; }
        public List<PersistCharacterCommand> PublishedCommands { get; } = [];
        public CharacterPersistenceService Service { get; }
        public Exception? PublishException { get; set; }
        public Exception? SaveChangesException { get; set; }

        public async ValueTask DisposeAsync()
        {
            _mapperProvider.Dispose();
            await _dbContext.DisposeAsync();
        }
    }

    private static DbContextOptions<HagalazDbContext> CreateDbContextOptions() =>
        new DbContextOptionsBuilder<HagalazDbContext>()
            .UseMySQL("Server=localhost;Database=hagalaz;User=root;Password=;")
            .Options;
}
