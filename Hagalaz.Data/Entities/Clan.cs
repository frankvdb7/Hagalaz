using System;
using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class Clan
    {
        public Clan()
        {
            ClansMembers = new HashSet<ClansMember>();
            Masters = new HashSet<Character>();
        }

        public uint Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime? CreationDate { get; set; }

        public virtual ClansSetting ClansSetting { get; set; } = null!;
        public virtual ICollection<ClansMember> ClansMembers { get; set; }

        public virtual ICollection<Character> Masters { get; set; }
    }
}
