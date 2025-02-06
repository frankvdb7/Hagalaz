using System;

namespace Hagalaz.Data.Entities
{
    public partial class GameEvent
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
