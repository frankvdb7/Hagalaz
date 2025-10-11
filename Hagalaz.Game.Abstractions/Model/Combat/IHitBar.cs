namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines the contract for a hit bar, which visually represents a creature's health status.
    /// </summary>
    public interface IHitBar
    {
        /// <summary>
        /// Gets the visual type of the hit bar.
        /// </summary>
         HitBarType Type { get; }

        /// <summary>
        /// Gets the delay before the hit bar appears or updates.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Gets the speed at which the hit bar animates to its new value.
        /// </summary>
        int Speed { get; }

        /// <summary>
        /// Gets the starting health value for the hit bar's animation.
        /// </summary>
        int CurrentLifePoints { get; }

        /// <summary>
        /// Gets the ending health value for the hit bar's animation.
        /// </summary>
        int NewLifePoints { get; }
    }
}
