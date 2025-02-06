using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetMiniMapTypeMessage : RaidoMessage
    {
        public required MinimapType MinimapType { get; init; }
    }
}
