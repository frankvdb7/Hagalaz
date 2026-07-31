using Hagalaz.Characters.Messages.Model;

namespace Hagalaz.Characters.Messages;

/// <summary>
/// Durable, one-way character snapshot persistence command.
/// The character service applies commands idempotently by snapshot revision.
/// </summary>
public record PersistCharacterCommand(
    Guid CorrelationId,
    uint MasterId,
    AppearanceDto Appearance,
    DetailsDto Details,
    StatisticsDto Statistics,
    ItemCollectionDto ItemCollection,
    FamiliarDto? Familiar,
    MusicDto Music,
    FarmingDto Farming,
    SlayerDto Slayer,
    NotesDto Notes,
    ProfileDto Profile,
    ItemAppearanceCollectionDto ItemAppearanceCollection,
    StateDto State,
    long SnapshotRevision) : ICharacterPersistenceMessage;
