namespace Hagalaz.Game.Abstractions.Features.Chat
{
    /// <summary>
    /// Defines a character's availability for multiple situations.
    /// </summary>
    public enum Availability : int
    {
        /// <summary>
        /// The character is available to everyone.
        /// </summary>
        Everyone = 0,
        /// <summary>
        /// The character is only available to friends.
        /// </summary>
        Friends = 1,
        /// <summary>
        /// The account is not available to anyone.
        /// </summary>
        Nobody = 2,
    }
}