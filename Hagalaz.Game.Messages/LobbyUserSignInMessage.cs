namespace Hagalaz.Game.Messages
{
    public record LobbyUserSignInMessage(
        uint MasterId,
        int WorldId,
        string WorldInstanceId,
        long WorldGeneration,
        long SessionGeneration,
        string ConnectionId);
}
