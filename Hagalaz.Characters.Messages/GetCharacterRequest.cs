namespace Hagalaz.Characters.Messages
{
    public record GetCharacterRequest(Guid CorrelationId, uint MasterId);
}