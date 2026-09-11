using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers;

public sealed class CharacterPersistenceAcknowledgedConsumer : IConsumer<PersistCharacterAcknowledged>
{
    private readonly ICharacterPersistenceService _persistenceService;

    public CharacterPersistenceAcknowledgedConsumer(ICharacterPersistenceService persistenceService) =>
        _persistenceService = persistenceService;

    public Task Consume(ConsumeContext<PersistCharacterAcknowledged> context)
    {
        if (context.Message.Outcome is not { } outcome)
        {
            return Task.CompletedTask;
        }

        _persistenceService.Acknowledge(
            context.Message.MasterId,
            context.Message.CorrelationId,
            context.Message.SnapshotRevision,
            outcome);
        return Task.CompletedTask;
    }
}
