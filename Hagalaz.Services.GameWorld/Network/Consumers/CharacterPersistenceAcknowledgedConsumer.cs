using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers;

public sealed class CharacterPersistenceAcknowledgedConsumer : IConsumer<PersistCharacterAcknowledged>
{
    private readonly ICharacterLogoutService _characterLogoutService;

    public CharacterPersistenceAcknowledgedConsumer(ICharacterLogoutService characterLogoutService) =>
        _characterLogoutService = characterLogoutService;

    public Task Consume(ConsumeContext<PersistCharacterAcknowledged> context) =>
        _characterLogoutService.AcknowledgeAndCompleteAsync(
            context.Message.MasterId,
            context.Message.SnapshotRevision,
            context.CancellationToken,
            context.Message.Outcome);
}
