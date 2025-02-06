using System;

namespace Hagalaz.Data.Entities
{
    public partial class LogsActivity
    {
        public ulong Id { get; set; }
        public uint MasterId { get; set; }
        public DateTime Date { get; set; }
        public string ShortDesc { get; set; } = null!;
        public string FullDesc { get; set; } = null!;
    }
}
