namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different display modes or screen layouts that the game client can be in.
    /// </summary>
    public enum DisplayMode : byte
    {
        /// <summary>
        /// The character is in the game lobby, not the main game world.
        /// </summary>
        LobbyScreen = 0,
        /// <summary>
        /// The game is displayed in a fixed-size window.
        /// </summary>
        FixedScreen = 1,
        /// <summary>
        /// The game is displayed in a resizable window.
        /// </summary>
        ResizedScreen = 2,
        /// <summary>
        /// The game is displayed in full-screen mode.
        /// </summary>
        FullScreen = 3,
        /// <summary>
        /// The character is on the new account registration screen.
        /// </summary>
        RegistrationScreen = 4,
    }
}
