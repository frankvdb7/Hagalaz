using System;
using System.IO;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameUpdate.Network.Messages
{
    public class PriorityFileResponse : RaidoMessage
    {
        public MemoryStream Data { get; init; } = default!;
        public byte IndexId { get; init; }
        public int FileId { get; init; }
        public bool HighPriority { get; init; }
        public byte EncryptionFlag { get; init; }
    }
}