namespace Hagalaz.Game.Abstractions.Builders.Request
{
    public interface IChatMessageRequestTargetMessage
    {
        IChatMessageRequestType WithTargetMessage(string message);
    }
}