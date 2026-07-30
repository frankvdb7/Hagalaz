using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
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
    public async Task PersistAsync_PublishesDurableOneWayCommandAndSkipsUnchangedSnapshot()
    {
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.DehydrateAsync(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>())
            .Returns(Task.FromResult(new CharacterModel()));
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        PersistCharacterCommand? publishedCommand = null;
        publishEndpoint
            .When(endpoint => endpoint.Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => publishedCommand = callInfo.Arg<PersistCharacterCommand>());

        await using var outboxDbContext = CreateOutboxDbContext();
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
            outboxDbContext,
            dehydrationService,
            new SnapshotRevisionGenerator(),
            new CharacterPersistenceState());

        await service.PersistAsync(character, force: false);
        await service.PersistAsync(character, force: false);

        Assert.IsNotNull(publishedCommand);
        Assert.AreEqual(42u, publishedCommand.MasterId);
        Assert.IsGreaterThan(0L, publishedCommand.SnapshotRevision);
        await publishEndpoint.Received(1).Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task PersistAsync_UsesNewRevisionWhenForced()
    {
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.DehydrateAsync(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>())
            .Returns(Task.FromResult(new CharacterModel()));
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        await using var outboxDbContext = CreateOutboxDbContext();
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
            outboxDbContext,
            dehydrationService,
            new SnapshotRevisionGenerator(),
            new CharacterPersistenceState());

        await service.PersistAsync(character, force: true);
        await service.PersistAsync(character, force: true);

        await publishEndpoint.Received(2).Publish(Arg.Any<PersistCharacterCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void OutboxDbContext_ContainsMassTransitInboxAndOutboxEntities()
    {
        using var context = CreateOutboxDbContext();
        var tableNames = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("InboxState", tableNames);
        Assert.Contains("OutboxMessage", tableNames);
        Assert.Contains("OutboxState", tableNames);
    }

    private static CharacterPersistenceOutboxDbContext CreateOutboxDbContext()
    {
        var options = new DbContextOptionsBuilder<CharacterPersistenceOutboxDbContext>()
            .UseMySQL("Server=localhost;Database=hagalaz;User=root;Password=;")
            .Options;
        return new CharacterPersistenceOutboxDbContext(options);
    }
}
