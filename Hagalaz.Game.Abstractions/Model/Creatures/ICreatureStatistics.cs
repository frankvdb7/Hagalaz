namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreatureStatistics
    {
        /// <summary>
        /// Contains creature hitpoints.
        /// </summary>
        /// <value>The constitution points.</value>
        int LifePoints { get; }
        /// <summary>
        /// Get's if player is poisoned.
        /// </summary>
        /// <value><c>true</c> if poisoned; otherwise, <c>false</c>.</value>
        bool Poisoned { get; }
        /// <summary>
        /// Gets the bonuses.
        /// </summary>
        /// <value>
        /// The bonuses.
        /// </value>
        IBonuses Bonuses { get; }
        /// <summary>
        /// Gets the prayer bonuses.
        /// </summary>
        /// <value>
        /// The prayer bonuses.
        /// </value>
        IBonusesPrayer PrayerBonuses { get; }
        /// <summary>
        /// Damage's creature hitpoints.
        /// creature.OnDeath() is possible.
        /// </summary>
        /// <param name="amount">Amount of damage.</param>
        /// <returns>
        /// Return's the actual amount of damage.
        /// </returns>
        int DamageLifePoints(int amount);
        /// <summary>
        /// Heal's creature hitpoints by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal hitpoints.</param>
        /// <returns>Returns the amount of points healed actually.</returns>
        int HealLifePoints(int amount);
        /// <summary>
        /// Get's level of specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception"></exception>
        int GetSkillLevel(int skillID);
        /// <summary>
        /// Damage's (Decreases) specific skill.
        /// </summary>
        /// <param name="skillID">The skill Id.</param>
        /// <param name="damage">The damage.</param>
        /// <returns>Returns the actual damage.</returns>
        int DamageSkill(int skillID, int damage);
        /// <summary>
        /// Heals (Increases) specific skill.
        /// </summary>
        /// <param name="skillID">The skill Id.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>Returns the actual heal amount.</returns>
        int HealSkill(int skillID, int amount);
        /// <summary>
        /// Set's character poison amount.
        /// </summary>
        /// <param name="amount">The amount.</param>
        void SetPoisonAmount(int amount);
        /// <summary>
        /// Increase's dynamic prayer bonus of specific type by 1.
        /// Does not work if current prayer bonus is higher or equal to 15.
        /// Type must be curse bonus!
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="max">The max.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool IncreaseCursePrayerBonus(BonusPrayerType type, int max);
        /// <summary>
        /// Decrease's dynamic prayer bonus of specific type by 1.
        /// Does not work if current bonus is lower or equal to maximum.
        /// Type must be curse bonus!
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="max">The max.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool DecreaseCursePrayerBonus(BonusPrayerType type, int max);
        /// <summary>
        /// Set's instant curse prayer bonus.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="value">The value.</param>
        void SetInstantCursePrayerBonus(BonusPrayerType type, int value);
        /// <summary>
        /// Set's turmoil bonuses according to target's skills.
        /// </summary>
        /// <param name="target">The target.</param>
        void SetTurmoilBonuses(ICreature target);
        /// <summary>
        /// Reset's all turmoil bonuses.
        /// </summary>
        void ResetTurmoilBonuses();
        /// <summary>
        /// Reset's all skill levels , hitpoints, prayerpoints
        /// to their full values.
        /// </summary>
        void Normalise();
        /// <summary>
        /// Ticks this instance.
        /// </summary>
        void Tick();
    }
}
