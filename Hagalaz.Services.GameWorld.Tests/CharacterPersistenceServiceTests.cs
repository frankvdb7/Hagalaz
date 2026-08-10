using System;
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
    public async Task PersistAsync_RedrivesUnacknowledgedSnapshotUntilAcknowledged()
    {
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.DehydrateAsync(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>())
            .Returns(Task.FromResult(new CharacterModel()));
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
            dehydrationService,
            state);

        service.InitializeRevision(42, 100);

        await service.PersistAsync(character, force: false);
        await service.PersistAsync(character, force: false);

        Assert.HasCount(2, publishedCommands);
        Assert.AreEqual(42u, publishedCommands[0].MasterId);
        Assert.AreEqual(101L, publishedCommands[0].SnapshotRevision);
        Assert.AreEqual(102L, publishedCommands[1].SnapshotRevision);

        state.Acknowledge(42, publishedCommands[1].CorrelationId, publishedCommands[1].SnapshotRevision);
        await service.PersistAsync(character, force: false);

        await publishEndpoint.Received(2).Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task PersistAsync_UsesNewRevisionWhenForced()
    {
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.DehydrateAsync(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>())
            .Returns(Task.FromResult(new CharacterModel()));
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        await using var dbContext = CreateSharedDbContext();
        using var mapperProvider = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(configuration => configuration.AddProfile<CharacterProfile>())
            .BuildServiceProvider();
        var mapper = mapperProvider.GetRequiredService<AutoMapper.IMapper>();
        var character = Substitute.For<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>();
        character.MasterId.Returns(42u);
        var service = new CharacterPersistenceService(
            NullLogger<CharacterPersistenceService>.Instance,
            mapper,
            publishEndpoint,
            dbContext,
            dehydrationService,
            new CharacterPersistenceState());

        await service.PersistAsync(character, force: true);
        await service.PersistAsync(character, force: true);

        await publishEndpoint.Received(2).Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>());
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
}
