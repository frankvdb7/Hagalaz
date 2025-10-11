namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines a contract for a collection of combat bonuses specifically granted by prayers and curses.
    /// </summary>
    public interface IBonusesPrayer : IBonuses<BonusPrayerType>
    {
        /// <summary>
        /// Increases the value of a specific prayer bonus by a given amount.
        /// </summary>
        /// <param name="type">The type of the prayer bonus to modify.</param>
        /// <param name="amount">The amount to add to the bonus.</param>
        void AddToBonus(BonusPrayerType type, int amount);

        /// <summary>
        /// Decreases the value of a specific prayer bonus by a given amount.
        /// </summary>
        /// <param name="type">The type of the prayer bonus to modify.</param>
        /// <param name="amount">The amount to remove from the bonus.</param>
        void RemoveFromBonus(BonusPrayerType type, int amount);
    }
}
