using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.Characters.Metrics;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.Characters.Consumers;

/// <summary>
/// Emits an operator-visible alert when the retry policy has exhausted a durable
/// character persistence command. MassTransit also moves the original command to
/// the endpoint error queue for redrive or inspection.
/// </summary>
public sealed class CharacterPersistenceFaultConsumer : IConsumer<Fault<PersistCharacterCommand>>
{
    private readonly ILogger<CharacterPersistenceFaultConsumer> _logger;
    private readonly CharacterPersistenceMetrics _metrics;

    public CharacterPersistenceFaultConsumer(
        ILogger<CharacterPersistenceFaultConsumer> logger,
        CharacterPersistenceMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public Task Consume(ConsumeContext<Fault<PersistCharacterCommand>> context)
    {
        var message = context.Message.Message;
        var exception = context.Message.Exceptions.FirstOrDefault();
        _metrics.RecordFailure();
        _logger.LogCritical(
            "Character persistence command exhausted retries for character {MasterId}, revision {SnapshotRevision}, correlation {CorrelationId}. Failure {ExceptionType}: {ExceptionMessage}. Redrive the UpdateCharacterRequest_error queue after resolving the cause.",
            message.MasterId,
            message.SnapshotRevision,
            message.CorrelationId,
            exception?.ExceptionType,
            exception?.Message);
        return Task.CompletedTask;
    }
}
