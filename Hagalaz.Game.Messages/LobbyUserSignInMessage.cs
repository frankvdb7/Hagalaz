namespace Hagalaz.Game.Messages
{
    public record LobbyUserSignInMessage(uint MasterId, int WorldId, long SessionGeneration, string ConnectionId);
}
