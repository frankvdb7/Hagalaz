using System;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.GameWorld.Network.Consumers;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterPersistenceAcknowledgedConsumerTests
{
    [TestMethod]
    public async Task Consume_ForwardsExactAcknowledgementToPersistenceService()
    {
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var consumer = new CharacterPersistenceAcknowledgedConsumer(persistenceService);
        var correlationId = Guid.NewGuid();
        var context = Substitute.For<ConsumeContext<PersistCharacterAcknowledged>>();
        context.Message.Returns(new PersistCharacterAcknowledged(
            correlationId,
            42,
            7,
            CharacterPersistenceOutcome.Committed));

        await consumer.Consume(context);

        persistenceService.Received(1).Acknowledge(
            42,
            correlationId,
            7,
            CharacterPersistenceOutcome.Committed);
    }

    [TestMethod]
    public async Task Consume_WithoutOutcomeDoesNotCompleteLogoutOrAcknowledge()
    {
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var consumer = new CharacterPersistenceAcknowledgedConsumer(persistenceService);
        var context = Substitute.For<ConsumeContext<PersistCharacterAcknowledged>>();
        context.Message.Returns(new PersistCharacterAcknowledged(Guid.NewGuid(), 42, 7));

        await consumer.Consume(context);

        persistenceService.DidNotReceive().Acknowledge(
            Arg.Any<uint>(),
            Arg.Any<Guid>(),
            Arg.Any<long>(),
            Arg.Any<CharacterPersistenceOutcome>());
    }
}
