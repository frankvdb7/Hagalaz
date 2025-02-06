using Microsoft.AspNetCore.Identity;

namespace Hagalaz.Data.Entities
{
    public partial class Aspnetuserclaim : IdentityUserClaim<uint>
    {
        public virtual Character User { get; set; } = null!;
    }
}
