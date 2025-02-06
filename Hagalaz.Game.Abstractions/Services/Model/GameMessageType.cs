namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public enum GameMessageType
    {
        /// <summary>
        /// The specific world announcement.
        /// These announcements will appear to everyone who shares the character's world at the time. They will
        /// also appear to all of the character's friends regardless of what world they are on.
        /// </summary>
        WorldSpecific,

        /// <summary>
        /// The world wide announcement.
        /// These announcements will appear to everyone on all worlds across the game.
        /// </summary>
        WorldWide,

        /// <summary>
        /// The game announcement. 
        /// These announcements are game-related news that inform the character of an upcoming event.
        /// </summary>
        Game,

        /// <summary>
        /// The friends announcement.
        /// These announcements are sent to friends only.
        /// </summary>
        Friends,
    }
}