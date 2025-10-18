namespace Hagalaz.Authorization.Constants
{
    /// <summary>
    /// Defines the constant values for user roles within the application.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// The character used to separate multiple role names in a string.
        /// </summary>
        public const string Separator = ",";

        /// <summary>
        /// The role for a system administrator, with the highest level of permissions across the entire application.
        /// </summary>
        public const string SystemAdministrator = "SystemAdministrator";

        /// <summary>
        /// The role for a game administrator, with high-level permissions within the game world.
        /// </summary>
        public const string GameAdministrator = "GameAdministrator";

        /// <summary>
        /// The role for a game moderator, with permissions to manage in-game community and enforce rules.
        /// </summary>
        public const string GameModerator = "GameModerator";

        /// <summary>
        /// The role for a player who has donated, potentially granting them special perks or access.
        /// </summary>
        public const string Donator = "Donator";
    }
}
