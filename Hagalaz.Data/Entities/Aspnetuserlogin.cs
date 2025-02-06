using Microsoft.AspNetCore.Identity;

namespace Hagalaz.Data.Entities
{
    public partial class Aspnetuserlogin : IdentityUserLogin<uint>
    {
        public virtual Character User { get; set; } = null!;
    }
}
