using Hagalaz.Game.Scripts.Model;

namespace Hagalaz.Game.Scripts.World
{
    /// <summary>
    /// </summary>
    public class ShootingStarEvent : WorldScriptBase
    {
        /// <summary>
        ///     The tick count.
        /// </summary>
        private int _tickCount;

        /// <summary>
        ///     Get's called when this script is created.
        /// </summary>
        public override void Initialize()
        {
        }

        /// <summary>
        ///     Get's called by major updater.
        /// </summary>
        public override void Tick()
        {
            if (++_tickCount >= 12000) // 120 minutes
            {
                // TODO - Spawn shooting star.
                _tickCount = 0;
            }
        }
    }
}