using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class ClientInfoMessage : RaidoMessage
    {
        public required int FramesPerSecond { get; init; }
        public required int GarbageCollectionTime { get; init; }
        public required int Ping { get; init; }
    }
}