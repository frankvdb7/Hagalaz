namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Contains creature glow render.
    /// </summary>
    public interface IGlow
    {
        /// <summary>
        /// Contains glow color 1.
        /// </summary>
        int Alpha { get; }
        /// <summary>
        /// Contains glow color 2.
        /// </summary>
        int Red { get; }
        /// <summary>
        /// Contains glow color 3.
        /// </summary>
        int Green { get; }
        /// <summary>
        /// Contains glow color 4.
        /// </summary>
        int Blue { get; }
        /// <summary>
        /// Contains delay of the glow.
        /// 1 = 30ms , 2 = 60ms , 3 = 90ms n so on.
        /// </summary>
        int Delay { get; }
        /// <summary>
        /// Contains duration of the glow.
        /// 1 = 30ms , 2 = 60ms , 3 = 90ms n so on.
        /// </summary>
        int Duration { get; }
    }
}
