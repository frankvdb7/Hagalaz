using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Providers
{
    public interface IClientPermissionProvider
    {
        int GetClientPermission(IList<string> roles);
    }
}