using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetClientChatTypeMessage : RaidoMessage
    {
        public required ClientChatType Type { get; init; }
    }
}
