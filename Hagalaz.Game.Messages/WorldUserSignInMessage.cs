namespace Hagalaz.Game.Messages
{
    public record WorldUserSignInMessage(uint MasterId, int WorldId, long SessionGeneration, string ConnectionId);
}
