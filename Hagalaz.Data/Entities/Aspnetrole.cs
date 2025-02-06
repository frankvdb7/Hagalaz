using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Hagalaz.Data.Entities
{
    public partial class Aspnetrole : IdentityRole<uint>
    {
        public Aspnetrole()
        {
            Aspnetuserroles = new HashSet<Aspnetuserrole>();
            Aspnetroleclaims = new HashSet<Aspnetroleclaim>();
            Users = new HashSet<Character>();
        }

        public new uint Id { get; set; }
        public new string? Name { get; set; }
        public new string? NormalizedName { get; set; }
        public new string? ConcurrencyStamp { get; set; }

        public virtual ICollection<Aspnetroleclaim> Aspnetroleclaims { get; set; }
        
        public virtual ICollection<Aspnetuserrole> Aspnetuserroles { get; set; }

        public virtual ICollection<Character> Users { get; set; }
    }
}
