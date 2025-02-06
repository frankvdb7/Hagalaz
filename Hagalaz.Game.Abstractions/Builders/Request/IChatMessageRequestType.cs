using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    public interface IChatMessageRequestType
    {
        IChatMessageRequestOption WithType(ChatMessageType type);
    }
}