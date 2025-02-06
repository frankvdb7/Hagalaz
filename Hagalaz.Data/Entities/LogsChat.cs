using System;

namespace Hagalaz.Data.Entities
{
    public partial class LogsChat
    {
        public ulong Id { get; set; }
        public uint MasterId { get; set; }
        public DateTime? Date { get; set; }
        public byte Type { get; set; }
        public uint ReceiverId { get; set; }
        public string Message { get; set; } = null!;
    }
}
