using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class ScreenChangedEvent : CharacterEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public DisplayMode DisplayMode { get; }
        /// <summary>
        /// 
        /// </summary>
        public int ScreenSizeY { get; }
        /// <summary>
        /// 
        /// </summary>
        public int ScreenSizeX { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="displayMode"></param>
        /// <param name="screenSizeX"></param>
        /// <param name="screenSizeY"></param>
        public ScreenChangedEvent(ICharacter target, DisplayMode displayMode, int screenSizeX, int screenSizeY) : base(target)
        {
            DisplayMode = displayMode;
            ScreenSizeY = screenSizeY;
            ScreenSizeX = screenSizeX;
        }
    }
}