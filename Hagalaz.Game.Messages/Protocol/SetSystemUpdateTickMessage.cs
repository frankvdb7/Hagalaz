using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetSystemUpdateTickMessage : RaidoMessage
    {
        public required int Tick { get; init; }
    }
}
