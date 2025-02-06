using System;

namespace Hagalaz.Data.Entities
{
    public partial class CharactersReport
    {
        public uint Id { get; set; }
        public uint ReporterId { get; set; }
        public uint ReportedId { get; set; }
        public byte Type { get; set; }
        public DateTime Date { get; set; }

        public virtual Character Reported { get; set; } = null!;
        public virtual Character Reporter { get; set; } = null!;
    }
}
