using System;

namespace Hagalaz.Data.Entities
{
    /// <summary>
    /// Player System
    /// </summary>
    public partial class CharactersTicket
    {
        public uint Id { get; set; }
        public uint MasterId { get; set; }
        public string TicketText { get; set; } = null!;
        public string? ResponseText { get; set; }
        public DateTime TicketLastchange { get; set; }
        public uint ModeratorId { get; set; }
        public byte Completed { get; set; }
    }
}
