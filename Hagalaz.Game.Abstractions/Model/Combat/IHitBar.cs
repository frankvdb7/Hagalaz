namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHitBar
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
         HitBarType Type { get; }
        /// <summary>
        /// Gets the delay.
        /// </summary>
        /// <value>
        /// The delay.
        /// </value>
        int Delay { get; }
        /// <summary>
        /// The speed of updating changes (in 20ms cycles).
        /// </summary>
        /// <value>
        /// The speed.
        /// </value>
        int Speed { get; }
        /// <summary>
        /// Gets the current life points.
        /// </summary>
        /// <value>
        /// The current life points.
        /// </value>
        int CurrentLifePoints { get; }
        /// <summary>
        /// Gets the new life points.
        /// </summary>
        /// <value>
        /// The new life points.
        /// </value>
        int NewLifePoints { get; }
    }
}
