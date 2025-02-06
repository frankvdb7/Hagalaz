using System;

namespace Hagalaz.Data.Entities
{
    public partial class Blacklist
    {
        public uint Id { get; set; }
        public string IpAddress { get; set; } = null!;
        public uint ModeratorId { get; set; }
        public string Reason { get; set; } = null!;
        public DateTime Date { get; set; }
        public uint AppealId { get; set; }

        public virtual CharactersAppeal Appeal { get; set; } = null!;
        public virtual Character Moderator { get; set; } = null!;
    }
}
