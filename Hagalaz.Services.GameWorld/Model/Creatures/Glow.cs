using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Contains character glow definition.
    /// </summary>
    public struct Glow : IGlow
    {
        /// <summary>
        /// Contains glow color 1.
        /// </summary>
        public int Alpha { get; set; }

        /// <summary>
        /// Contains glow color 2.
        /// </summary>
        public int Red { get; set; }

        /// <summary>
        /// Contains glow color 3.
        /// </summary>
        public int Green { get; set; }

        /// <summary>
        /// Contains glow color 4.
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// Contains delay of the glow.
        /// 1 = 30ms , 2 = 60ms , 3 = 90ms n so on.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// Contains duration of the glow.
        /// 1 = 30ms , 2 = 60ms , 3 = 90ms n so on.
        /// </summary>
        public int Duration { get; set; }
    }
}