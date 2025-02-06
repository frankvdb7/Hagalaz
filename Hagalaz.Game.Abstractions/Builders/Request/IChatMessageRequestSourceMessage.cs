namespace Hagalaz.Game.Abstractions.Builders.Request
{
    public interface IChatMessageRequestSourceMessage
    {
        IChatMessageRequestTarget WithSourceMessage(string message);
    }
}