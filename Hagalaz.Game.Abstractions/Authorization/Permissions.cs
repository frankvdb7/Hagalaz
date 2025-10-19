using System;

namespace Hagalaz.Game.Abstractions.Authorization
{
    /// <summary>
    /// Defines a set of bitwise flags representing user authorization levels within the game.
    /// Higher-level permissions inherit the rights of lower levels.
    /// </summary>
    [Flags]
    public enum Permission
    {
        /// <summary>
        /// Represents a standard player with no special privileges. This is the default permission level.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Represents a system administrator with the highest level of access, including all game administration, moderation, and donator privileges.
        /// </summary>
        SystemAdministrator = (1 << 1) | GameAdministrator | GameModerator | Donator,

        /// <summary>
        /// Represents a game administrator with broad access to game commands and moderation tools. Inherits all moderator and donator privileges.
        /// </summary>
        GameAdministrator = (1 << 2) | GameModerator | Donator,

        /// <summary>
        /// Represents a game moderator with abilities such as muting players and teleporting. Inherits all donator privileges.
        /// </summary>
        GameModerator = (1 << 3) | Donator,

        /// <summary>
        /// Represents a donator with access to certain exclusive features or benefits.
        /// </summary>
        Donator = 1 << 4
    }

    /// <summary>
    /// Provides helper methods for working with the <see cref="Permission"/> enum.
    /// </summary>
    public static class PermissionHelpers
    {
        /// <summary>
        /// Parses a string representation of a role into its corresponding <see cref="Permission"/> enum value.
        /// </summary>
        /// <param name="role">The string name of the role to parse (e.g., "GameAdministrator"). The comparison is case-sensitive.</param>
        /// <returns>
        /// The <see cref="Permission"/> value that matches the role string. If the string does not match any defined role,
        /// it defaults to <see cref="Permission.Standard"/>.
        /// </returns>
        public static Permission ParseRole(string role)
        {
            if (Enum.TryParse(role, true, out Permission parsedPermission))
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
