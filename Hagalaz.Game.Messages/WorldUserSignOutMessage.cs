namespace Hagalaz.Game.Messages
{
    public record WorldUserSignOutMessage(uint MasterId, int WorldId, string ConnectionId);
}
