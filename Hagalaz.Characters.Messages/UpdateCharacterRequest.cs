using Hagalaz.Characters.Messages.Model;

namespace Hagalaz.Characters.Messages
{
    public record UpdateCharacterRequest(Guid CorrelationId,
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
                                       StateDto State);
}
