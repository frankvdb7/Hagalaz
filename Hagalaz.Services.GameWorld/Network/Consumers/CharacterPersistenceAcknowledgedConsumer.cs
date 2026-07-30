using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers;

public sealed class CharacterPersistenceAcknowledgedConsumer : IConsumer<PersistCharacterAcknowledged>
{
    private readonly CharacterPersistenceState _state;

    public CharacterPersistenceAcknowledgedConsumer(CharacterPersistenceState state) => _state = state;

    public Task Consume(ConsumeContext<PersistCharacterAcknowledged> context)
    {
        var message = context.Message;
        _state.Acknowledge(message.MasterId, message.SnapshotRevision);
        return Task.CompletedTask;
    }
}
