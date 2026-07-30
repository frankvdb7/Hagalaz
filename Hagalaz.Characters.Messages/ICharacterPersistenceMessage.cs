using Hagalaz.Characters.Messages.Model;

namespace Hagalaz.Characters.Messages;

public interface ICharacterPersistenceMessage
{
    Guid CorrelationId { get; }
    uint MasterId { get; }
    AppearanceDto Appearance { get; }
    DetailsDto Details { get; }
    StatisticsDto Statistics { get; }
    ItemCollectionDto ItemCollection { get; }
    FamiliarDto? Familiar { get; }
    MusicDto Music { get; }
    FarmingDto Farming { get; }
    SlayerDto Slayer { get; }
    NotesDto Notes { get; }
    ProfileDto Profile { get; }
    ItemAppearanceCollectionDto ItemAppearanceCollection { get; }
    StateDto State { get; }
    long SnapshotRevision { get; }
}
