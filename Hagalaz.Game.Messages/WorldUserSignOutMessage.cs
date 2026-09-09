namespace Hagalaz.Game.Messages
{
    public record WorldUserSignOutMessage(uint MasterId, int WorldId, long SessionGeneration, string ConnectionId);
}
