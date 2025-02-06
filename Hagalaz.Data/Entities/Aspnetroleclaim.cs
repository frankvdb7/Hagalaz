using Microsoft.AspNetCore.Identity;

namespace Hagalaz.Data.Entities
{
    public partial class Aspnetroleclaim : IdentityRoleClaim<uint>
    {
        public virtual Aspnetrole Role { get; set; } = null!;
    }
}
