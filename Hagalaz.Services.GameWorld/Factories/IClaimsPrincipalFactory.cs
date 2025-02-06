using System.Collections.Generic;
using System.Security.Claims;

namespace Hagalaz.Services.GameWorld.Factories
{
    public interface IClaimsPrincipalFactory
    {
        public ClaimsPrincipal Create(IDictionary<string, object> Claims);
    }
}
