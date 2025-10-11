namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines a contract for a collection of standard combat bonuses.
    /// </summary>
    public interface IBonuses : IBonuses<BonusType>
    {
        /// <summary>
        /// Creates a new, identical copy of this bonuses collection.
        /// </summary>
        /// <returns>A new <see cref="IBonuses"/> instance with the same bonus values.</returns>
        IBonuses Copy();
    }

    /// <summary>
    /// Defines a generic contract for a collection of bonuses, where the type of bonus is specified by an enum.
    /// </summary>
    /// <typeparam name="TBonusType">The enum type that defines the different kinds of bonuses.</typeparam>
    public interface IBonuses<in TBonusType>
    {
        /// <summary>
        /// Adds the values from another bonus collection to this one.
        /// </summary>
        /// <param name="bonuses">The bonus collection to add.</param>
        void Add(IBonuses<TBonusType> bonuses);

        /// <summary>
        /// Subtracts the values from another bonus collection from this one.
        /// </summary>
        /// <param name="bonuses">The bonus collection to remove.</param>
        void Remove(IBonuses<TBonusType> bonuses);

        /// <summary>
        /// Gets the value of a specific bonus type.
        /// </summary>
        /// <param name="type">The type of the bonus to retrieve.</param>
        /// <returns>The integer value of the specified bonus.</returns>
        int GetBonus(TBonusType type);

        /// <summary>
        /// Sets the value of a specific bonus type.
        /// </summary>
        /// <param name="type">The type of the bonus to set.</param>
        /// <param name="value">The new value for the bonus.</param>
        void SetBonus(TBonusType type, int value);

        /// <summary>
        /// Resets all bonus values in this collection to zero.
        /// </summary>
        void Reset();
    }
}
