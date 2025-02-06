using Hagalaz.Game.Abstractions.Authorization;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class PermissionExtensions
    {
        /// <summary>
        /// Whether this character has been given a special permission or any
        /// other same type but higher level permission.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <param name="permission">The permission to check for.</param>
        /// <returns>
        ///   <c>true</c> if [has at least X permission] [the specified permission]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAtLeastXPermission(this Permission @this, Permission permission) => @this.HasFlag(permission);

        /// <summary>
        /// Generates title depending on character's rights.
        /// </summary>
        /// <returns>System.String.</returns>
        public static string ToRightsTitle(this Permission @this)
        {
            if (@this.HasFlag(Permission.SystemAdministrator))
                return "SYSTEM ADMINISTRATOR";
            else if (@this.HasFlag(Permission.GameAdministrator))
                return "GAME ADMINISTRATOR";
            else if (@this.HasFlag(Permission.GameModerator))
                return "GAME MODERATOR";
            else if (@this.HasFlag(Permission.Donator))
                return "DONATOR";
            else
                return "PLAYER";
        }

        /// <summary>
        /// Get's client rights byte depending on character's permissions.
        /// </summary>
        /// <returns>System.Byte.</returns>
        public static int ToClientRights(this Permission @this)
        {
            var b = 0;
            if (@this.HasFlag(Permission.GameAdministrator))
                b = 2;
            else if (@this.HasFlag(Permission.GameModerator))
                b = 1;
            else if (@this.HasFlag(Permission.Donator))
                b = 8;
            return b;
        }
    }
}
