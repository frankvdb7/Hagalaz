namespace Hagalaz.Game.Messages.Model
{
    /// <summary>
    ///     The type of login requested.
    /// </summary>
    public enum SessionType : byte
    {
        /// <summary>
        /// In-lobby status
        /// </summary>
        Lobby = 0,
        /// <summary>
        /// In-game status
        /// </summary>
        Game = 1,
    }
}