namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for managing a creature's statistics, including health, skills, and combat bonuses.
    /// </summary>
    public interface ICreatureStatistics
    {
        /// <summary>
        /// Gets the current life points (health) of the creature.
        /// </summary>
        int LifePoints { get; }

        /// <summary>
        /// Gets a value indicating whether the creature is currently poisoned.
        /// </summary>
        bool Poisoned { get; }

        /// <summary>
        /// Gets the collection of combat bonuses derived from the creature's equipment.
        /// </summary>
        IBonuses Bonuses { get; }

        /// <summary>
        /// Gets the collection of combat bonuses derived from the creature's active prayers and curses.
        /// </summary>
        IBonusesPrayer PrayerBonuses { get; }

        /// <summary>
        /// Reduces the creature's life points by a specified amount.
        /// </summary>
        /// <param name="amount">The amount of damage to inflict.</param>
        /// <returns>The actual amount of damage dealt.</returns>
        int DamageLifePoints(int amount);

        /// <summary>
        /// Increases the creature's life points by a specified amount.
        /// </summary>
        /// <param name="amount">The amount to heal.</param>
        /// <returns>The actual amount of life points restored.</returns>
        int HealLifePoints(int amount);

        /// <summary>
        /// Gets the current level of a specific skill.
        /// </summary>
        /// <param name="skillID">The ID of the skill to retrieve.</param>
        /// <returns>The level of the specified skill.</returns>
        int GetSkillLevel(int skillID);

        /// <summary>
        /// Reduces the level of a specific skill by a specified amount.
        /// </summary>
        /// <param name="skillID">The ID of the skill to damage.</param>
        /// <param name="damage">The amount to reduce the skill level by.</param>
        /// <returns>The actual amount the skill was damaged.</returns>
        int DamageSkill(int skillID, int damage);

        /// <summary>
        /// Increases the level of a specific skill by a specified amount.
        /// </summary>
        /// <param name="skillID">The ID of the skill to heal.</param>
        /// <param name="amount">The amount to increase the skill level by.</param>
        /// <returns>The actual amount the skill was healed.</returns>
        int HealSkill(int skillID, int amount);

        /// <summary>
        /// Sets the current poison strength for the creature.
        /// </summary>
        /// <param name="amount">The strength of the poison.</param>
        void SetPoisonAmount(int amount);

        /// <summary>
        /// Increases a dynamic curse bonus, up to a specified maximum.
        /// </summary>
        /// <param name="type">The type of curse bonus to increase.</param>
        /// <param name="max">The maximum value for the bonus.</param>
        /// <returns><c>true</c> if the bonus was increased; otherwise, <c>false</c>.</returns>
        bool IncreaseCursePrayerBonus(BonusPrayerType type, int max);

        /// <summary>
        /// Decreases a dynamic curse bonus.
        /// </summary>
        /// <param name="type">The type of curse bonus to decrease.</param>
        /// <param name="max">The maximum value for the bonus (used in calculation).</param>
        /// <returns><c>true</c> if the bonus was decreased; otherwise, <c>false</c>.</returns>
        bool DecreaseCursePrayerBonus(BonusPrayerType type, int max);

        /// <summary>
        /// Sets an instantaneous curse bonus to a specific value.
        /// </summary>
        /// <param name="type">The type of curse bonus to set.</param>
        /// <param name="value">The value to set.</param>
        void SetInstantCursePrayerBonus(BonusPrayerType type, int value);

        /// <summary>
        /// Sets the bonuses for the Turmoil curse based on the target's skill levels.
        /// </summary>
        /// <param name="target">The target creature whose stats will influence the Turmoil bonus.</param>
        void SetTurmoilBonuses(ICreature target);

        /// <summary>
        /// Resets all bonuses related to the Turmoil curse to zero.
        /// </summary>
        void ResetTurmoilBonuses();

        /// <summary>
        /// Restores all of the creature's stats (skills, life points, prayer) to their base levels.
        /// </summary>
        void Normalise();

        /// <summary>
        /// Processes a single game tick for the creature's statistics, handling effects like poison and prayer drain.
        /// </summary>
        void Tick();
    }
}
