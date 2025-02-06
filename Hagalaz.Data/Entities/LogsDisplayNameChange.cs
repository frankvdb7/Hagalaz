using System;

namespace Hagalaz.Data.Entities
{
    public partial class LogsDisplayNameChange
    {
        public ulong Id { get; set; }
        public uint MasterId { get; set; }
        public string PreviousDisplayName { get; set; } = null!;
        public string NewDisplayName { get; set; } = null!;
        public DateTime DateChanged { get; set; }
    }
}
