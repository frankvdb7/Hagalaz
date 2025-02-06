using System;

namespace Hagalaz.Data.Entities
{
    public partial class LogsLoginAttempt
    {
        public ulong Id { get; set; }
        public uint MasterId { get; set; }
        public DateTime? Date { get; set; }
        public string Ip { get; set; } = null!;
        public sbyte Attempt { get; set; }
        public sbyte Type { get; set; }
    }
}
