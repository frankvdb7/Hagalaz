namespace Hagalaz.Game.Scripts.Model
{
    /// <summary>
    /// Base class for world script.
    /// </summary>
    public abstract class WorldScriptBase
    {
        /// <summary>
        /// Get's called when this script is created.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Get's called by major updater.
        /// </summary>
        public abstract void Tick();
    }
}