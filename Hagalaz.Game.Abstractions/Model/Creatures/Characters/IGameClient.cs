namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGameClient
    {
        /// <summary>
        /// Contains display mode that is currently showed.
        /// </summary>
        /// <value>The display mode.</value>
        DisplayMode DisplayMode { get; set; }
        Language Language { get; set; }
        /// <summary>
        /// Contains screen size X ( height )
        /// </summary>
        /// <value>The screen size X.</value>
        int ScreenSizeX { get; set; }
        /// <summary>
        /// Contains screen size Y ( width )
        /// </summary>
        /// <value>The screen size Y.</value>
        int ScreenSizeY { get; set; }
        /// <summary>
        /// Get's if client window is currently active.
        /// </summary>
        /// <value><c>true</c> if this instance is window active; otherwise, <c>false</c>.</value>
        bool IsWindowActive { get; set; }
        /// <summary>
        /// Determines whether [is screen fixed].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is screen fixed]; otherwise, <c>false</c>.
        /// </returns>
        bool IsScreenFixed { get; }
    }
}
