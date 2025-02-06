namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="BonusType" />
    public interface IBonuses : IBonuses<BonusType>
    {
        /// <summary>
        /// Copies this bonuses.
        /// </summary>
        /// <returns></returns>
        IBonuses Copy();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TBonusType">The type of the bonus type.</typeparam>
    /// <seealso cref="Creatures.BonusType" />
    public interface IBonuses<TBonusType>
    {
        /// <summary>
        /// Add's specific bonuses to this bonuses class.
        /// </summary>
        /// <param name="bonuses">Bonuses which should be added.</param>
        void Add(IBonuses<TBonusType> bonuses);
        /// <summary>
        /// Remove's specific bonuses to this bonuses class.
        /// </summary>
        /// <param name="bonuses">Bonuses which should be removed.</param>
        void Remove(IBonuses<TBonusType> bonuses);
        /// <summary>
        /// Get's specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to get.</param>
        /// <returns></returns>
        int GetBonus(TBonusType type);
        /// <summary>
        /// Set's specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to set.</param>
        /// <param name="value">Value which should be seted.</param>
        void SetBonus(TBonusType type, int value);
        /// <summary>
        /// Set's all bonuses to 0.
        /// </summary>
        void Reset();
    }
}
