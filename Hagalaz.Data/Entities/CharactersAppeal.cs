using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class CharactersAppeal
    {
        public CharactersAppeal()
        {
            Blacklists = new HashSet<Blacklist>();
        }

        public uint Id { get; set; }
        public uint MasterId { get; set; }
        public string Data { get; set; } = null!;

        public virtual Character Master { get; set; } = null!;
        public virtual ICollection<Blacklist> Blacklists { get; set; }
    }
}
