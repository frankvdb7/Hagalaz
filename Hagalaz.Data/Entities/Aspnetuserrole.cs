using Microsoft.AspNetCore.Identity;

namespace Hagalaz.Data.Entities
{
    public partial class Aspnetuserrole : IdentityUserRole<uint>
    {
        public virtual Character User { get; set; } = null!;
        public virtual Aspnetrole Role { get; set; } = null!;
    }
}