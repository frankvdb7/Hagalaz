using System;

namespace Hagalaz.Data.Entities
{
    public partial class LogsConnection
    {
        public long Id { get; set; }
        public string Ip { get; set; } = null!;
        public DateTime? Time { get; set; }
        public int Port { get; set; }
    }
}
