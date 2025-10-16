namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for an object that holds information about the character's game client, such as display settings and language.
    /// </summary>
    public interface IGameClient
    {
        /// <summary>
        /// Gets or sets the current display mode of the game client.
        /// </summary>
        DisplayMode DisplayMode { get; set; }
        /// <summary>
        /// Gets or sets the language setting of the game client.
        /// </summary>
        Language Language { get; set; }
        /// <summary>
        /// Gets or sets the width of the game screen in pixels.
        /// </summary>
        int ScreenSizeX { get; set; }
        /// <summary>
        /// Gets or sets the height of the game screen in pixels.
        /// </summary>
        int ScreenSizeY { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the game client window is currently active (in focus).
        /// </summary>
        bool IsWindowActive { get; set; }
        /// <summary>
        /// Gets a value indicating whether the client is in a fixed-screen display mode.
        /// </summary>
        bool IsScreenFixed { get; }
    }
}
