namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="BonusPrayerType" />
    public interface IBonusesPrayer : IBonuses<BonusPrayerType>
    {
        /// <summary>
        /// Add's to specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to add to.</param>
        /// <param name="amount">Amount which should be added.</param>
        void AddToBonus(BonusPrayerType type, int amount);
        /// <summary>
        /// Remove's from specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to remove from.</param>
        /// <param name="amount">Amount which should be removed.</param>
        void RemoveFromBonus(BonusPrayerType type, int amount);
    }
}
