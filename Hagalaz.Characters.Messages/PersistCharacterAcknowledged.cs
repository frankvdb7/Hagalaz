namespace Hagalaz.Characters.Messages;

/// <summary>
/// Acknowledges that a character persistence command is durably applied or was
/// already applied at the requested snapshot revision.
/// </summary>
public record PersistCharacterAcknowledged(
    Guid CorrelationId,
    uint MasterId,
    long SnapshotRevision,
    CharacterPersistenceOutcome? Outcome = null);
