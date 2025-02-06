using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Represends game client that is connected to this server.
    /// </summary>
    public class GameClient : IGameClient
    {
        /// <summary>
        /// Contains display mode that is currently showed.
        /// </summary>
        /// <value>The display mode.</value>
        public DisplayMode DisplayMode { get; set; }

        /// <summary>
        /// Contains language that is currently showed to this client.
        /// </summary>
        /// <value>The language.</value>
        public Language Language { get; set; }

        /// <summary>
        /// Contains screen size X ( height )
        /// </summary>
        /// <value>The screen size X.</value>
        public int ScreenSizeX { get; set; }

        /// <summary>
        /// Contains screen size Y ( width )
        /// </summary>
        /// <value>The screen size Y.</value>
        public int ScreenSizeY { get; set; }

        /// <summary>
        /// Get's if client window is currently active.
        /// </summary>
        /// <value><c>true</c> if this instance is window active; otherwise, <c>false</c>.</value>
        public bool IsWindowActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is screen fixed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is screen fixed; otherwise, <c>false</c>.
        /// </value>
        public bool IsScreenFixed => DisplayMode != DisplayMode.FullScreen && DisplayMode != DisplayMode.ResizedScreen;

        /// <summary>
        /// Constructs new game client with given login details.
        /// </summary>
        /// <param name="displayMode">The display mode.</param>
        /// <param name="language">The language.</param>
        /// <param name="screenSizeX">The screen size x.</param>
        /// <param name="screenSizeY">The screen size y.</param>
        public GameClient(DisplayMode displayMode, Language language, int screenSizeX, int screenSizeY)
        {
            DisplayMode = displayMode;
            Language = language;
            ScreenSizeX = screenSizeX;
            ScreenSizeY = screenSizeY;
        }
    }
}