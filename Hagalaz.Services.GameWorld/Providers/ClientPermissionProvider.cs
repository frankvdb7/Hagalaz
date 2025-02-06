using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Authorization.Constants;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class ClientPermissionProvider : IClientPermissionProvider
    {
        public int GetClientPermission(IList<string> roles)
        {
            if (roles.Contains(Roles.GameModerator, StringComparer.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (roles.Contains(Roles.GameAdministrator, StringComparer.OrdinalIgnoreCase) || roles.Contains(Roles.SystemAdministrator, StringComparer.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (roles.Contains(Roles.Donator, StringComparer.OrdinalIgnoreCase))
            {
                return 8;
            }

            return 0;
        }
    }
}