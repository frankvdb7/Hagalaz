using Microsoft.AspNetCore.Identity;

namespace Hagalaz.Data.Entities
{
    public partial class Aspnetusertoken : IdentityUserToken<uint>
    {
        public virtual Character User { get; set; } = null!;
    }
}
