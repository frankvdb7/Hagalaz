namespace Hagalaz.Characters.Messages
{
    public record UpdateCharacterResponse(
        Guid CorrelationId,
        uint MasterId,
        CharacterPersistenceOutcome Outcome = CharacterPersistenceOutcome.Committed);
}
