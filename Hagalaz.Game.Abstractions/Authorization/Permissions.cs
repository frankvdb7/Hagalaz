using System;

namespace Hagalaz.Game.Abstractions.Authorization
{
    /// <summary>
    /// Contains permissions.
    /// </summary>
    [Flags]
    public enum Permission
    {
        /// <summary>
        /// Standard permissions.
        /// Has normal member permissions.
        /// </summary>
        Standard = 0,
        /// <summary>
        /// System admin permission.
        /// Has access to almost everything.
        /// </summary>
        SystemAdministrator = (1 << 1) | GameAdministrator | GameModerator | Donator,
        /// <summary>
        /// Game admin permission.
        /// Has access to almost every game command.
        /// </summary>
        GameAdministrator = (1 << 2) | GameModerator | Donator,
        /// <summary>
        /// Client moderator permission.
        /// Can mute/unmute people, also can teleport to other people.
        /// </summary>
        GameModerator = (1 << 3) | Donator,
        /// <summary>
        /// Donator permission.
        /// Can do certain things more than non-donator.
        /// </summary>
        Donator = 1 << 4
    }

    public static class PermissionHelpers
    {
        public static Permission ParseRole(string role)
        {
            if (Enum.TryParse(role, out Permission parsedPermission))
            {
                return parsedPermission;
            }
            else
            {
                return Permission.Standard; // Handle invalid roles as needed
            }
        }
    }
}
