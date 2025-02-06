using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class FocusEventMessage : RaidoMessage
    {
        public required bool Focussed { get; init; }
    }
}
