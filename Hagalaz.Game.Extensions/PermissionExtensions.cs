using Hagalaz.Game.Abstractions.Authorization;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="Permission"/> enum.
    /// </summary>
    public static class PermissionExtensions
    {
        /// <summary>
        /// Checks if a given permission set includes a specific permission level or a higher one.
        /// Since the permissions are flags and higher levels include lower ones, this is equivalent to <see cref="Enum.HasFlag"/>.
        /// </summary>
        /// <param name="this">The permission set to check.</param>
        /// <param name="permission">The permission level to check for.</param>
        /// <returns><c>true</c> if the permission set has at least the specified permission level; otherwise, <c>false</c>.</returns>
        public static bool HasAtLeastXPermission(this Permission @this, Permission permission) => @this.HasFlag(permission);

        /// <summary>
        /// Converts a permission level to a user-friendly title string.
        /// </summary>
        /// <param name="this">The permission level to convert.</param>
        /// <returns>A string representing the title associated with the highest permission level (e.g., "SYSTEM ADMINISTRATOR", "PLAYER").</returns>
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
        /// Converts a permission level to the corresponding integer value used by the game client to display crowns or icons.
        /// </summary>
        /// <param name="this">The permission level to convert.</param>
        /// <returns>An integer representing the client-side rights value.</returns>
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
