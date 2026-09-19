namespace Hagalaz.Game.Messages
{
    public record WorldUserSignInMessage(
        uint MasterId,
        int WorldId,
        string WorldInstanceId,
        long WorldGeneration,
        long SessionGeneration,
        string ConnectionId);
}
