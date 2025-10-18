namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the different types of global game messages, which determine their scope and audience.
    /// </summary>
    public enum GameMessageType
    {
        /// <summary>
        /// An announcement broadcast to everyone on a specific world and to all friends of the announcer.
        /// </summary>
        WorldSpecific,
        /// <summary>
        /// An announcement broadcast to everyone on all worlds.
        /// </summary>
        WorldWide,
        /// <summary>
        /// A standard game message, typically for system events or information.
        /// </summary>
        Game,
        /// <summary>
        /// An announcement broadcast only to the announcer's friends.
        /// </summary>
        Friends,
    }
}