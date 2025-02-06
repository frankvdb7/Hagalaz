using Raido.Common.Protocol;

namespace Hagalaz.Services.GameUpdate.Network.Messages
{
    public class PriorityFileRequest : RaidoMessage
    {
        public byte IndexId { get; init; }
        public int FileId { get; init; }
        public bool HighPriority { get; init; }
    }
}