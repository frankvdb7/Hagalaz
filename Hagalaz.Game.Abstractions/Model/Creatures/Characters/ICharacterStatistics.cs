using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Hagalaz.GameAbstractions.Model.Creatures.ICreatureStatistics" />
    public interface ICharacterStatistics : ICreatureStatistics
    {
        /// <summary>
        /// Contains character prayer points.
        /// </summary>
        /// <value>The prayer points.</value>
        int PrayerPoints { get; }
        /// <summary>
        /// Contains character special energy.
        /// </summary>
        /// <value>The special energy.</value>
        int SpecialEnergy { get; }
        /// <summary>
        /// Contains character run energy.
        /// </summary>
        /// <value>The run energy.</value>
        int RunEnergy { get; }
        /// <summary>
        /// Gets the character's combat level. (Excluding summoning)
        /// </summary>
        /// <value>The base combat level.</value>
        int BaseCombatLevel { get; }
        /// <summary>
        /// Gets the character's combat level. (Including summoning)
        /// </summary>
        /// <value>The full combat level.</value>
        int FullCombatLevel { get; }
        /// <summary>
        /// Get's character's total level.
        /// </summary>
        /// <value>The total level.</value>
        int TotalLevel { get; }
        /// <summary>
        /// Çontains the character total play time.
        /// </summary>
        TimeSpan PlayTime { get; }
        /// <summary>
        /// Set's xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <param name="xp">The xp.</param>
        /// <exception cref="System.Exception">Invalid counter Id value.</exception>
        void SetXpCounter(int counterID, double xp);
        /// <summary>
        /// Sets the tracked xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <param name="optionID">The skill identifier.</param>
        /// <exception cref="System.Exception">Invalid Id given.</exception>
        void SetTrackedXpCounter(int counterID, int optionID);
        /// <summary>
        /// Refreshe's character special energy.
        /// </summary>
        void RefreshSpecialEnergy();
        /// <summary>
        /// Refreshes the xp counters, including the refresh of
        /// -Enabled XP counters
        /// -Tracked XP counters
        /// -XP Counter value
        /// </summary>
        void RefreshXpCounters();
        /// <summary>
        /// Refreshe's hitpoints orb on client.
        /// TODO - update this in the interface, by events.
        /// </summary>
        void RefreshLifePoints();
        /// <summary>
        /// Normalises the boosted statistics.
        /// </summary>
        void NormalizeBoostedStatistics();
        /// <summary>
        /// Refreshe's poison.
        /// </summary>
        void RefreshPoison();
        /// <summary>
        /// Refreshe's prayer points.
        /// </summary>
        void RefreshPrayerPoints();
        /// <summary>
        /// Refreshe's run energy.
        /// </summary>
        void RefreshRunEnergy();
        /// <summary>
        /// Normalises the skills.
        /// </summary>
        /// <param name="skillIDs">The skills.</param>
        void NormaliseSkills(int[] skillIDs);
        /// <summary>
        /// Toggles the xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <exception cref="System.Exception">Invalid counter Id value.</exception>
        void ToggleXpCounter(int counterID);
        /// <summary>
        /// Add's skill experience.
        /// Experience is multiplied with the xp rate multiplier.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="experience">Amount of experience to add.</param>
        /// <returns>If the skill level was changed.</returns>
        /// <exception cref="System.Exception"></exception>
        bool AddExperience(int skillID, double experience);
        /// <summary>
        /// Set's skill level.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="level">Skill level.</param>
        /// <exception cref="System.Exception">Bad skillID</exception>
        void SetSkillLevel(int skillID, int level);
        /// <summary>
        /// Set's skill experience.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="experience">Skill experience.</param>
        /// <exception cref="System.Exception"></exception>
        void SetSkillExperience(int skillID, double experience);
        /// <summary>
        /// Gets the level for the amount of exp for the specified skill.
        /// </summary>
        /// <param name="skillID">The skill</param>
        /// <returns>System.Byte.</returns>
        int LevelForExperience(int skillID);
        /// <summary>
        /// Drain's run energy by specified amount.
        /// </summary>
        /// <param name="amount">Amount to drain.</param>
        /// <returns>Amount that was drained.</returns>
        int DrainRunEnergy(int amount);
        /// <summary>
        /// Drain's character's prayer points.
        /// </summary>
        /// <param name="amount">Amount of damage.</param>
        /// <returns>Return's the actual amount of damage.</returns>
        int DrainPrayerPoints(int amount);
        /// <summary>
        /// Get's character maximum prayer points.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetMaximumPrayerPoints();
        /// <summary>
        /// Drain's character special energy.
        /// </summary>
        /// <param name="amount">Amount of special energy to be drained.</param>
        /// <returns>Return's the actual amount of energy drained.</returns>
        int DrainSpecialEnergy(int amount);
        /// <summary>
        /// Sets the skill target level.
        /// </summary>
        /// <param name="skillID">The skill identifier.</param>
        /// <param name="level">The level.</param>
        /// <exception cref="System.Exception">Bad skillID</exception>
        void SetSkillTargetLevel(int skillID, int level);
        /// <summary>
        /// Sets the skill target experience.
        /// </summary>
        /// <param name="skillID">The skill identifier.</param>
        /// <param name="experience">The experience.</param>
        /// <exception cref="System.Exception">Bad skillID</exception>
        void SetSkillTargetExperience(int skillID, int experience);
        /// <summary>
        /// Stop's flashing specified skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <exception cref="System.Exception"></exception>
        void StopFlashingSkill(int skillID);
        /// <summary>
        /// Heal's run energy by specified amount.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Amount that was healed.</returns>
        int HealRunEnergy(int amount);
        /// <summary>
        /// Refreshe's skills.
        /// </summary>
        void RefreshSkills();
        /// <summary>
        /// Get's if specific skill is being flashed.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>If the skill is being flashed.</returns>
        /// <exception cref="System.Exception"></exception>
        bool SkillFlashed(int skillID);
        /// <summary>
        /// Refreshes the skill targets.
        /// </summary>
        void RefreshSkillTargets();
        /// <summary>
        /// Heal's character hitpoints by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal hitpoints.</param>
        /// <param name="max">Maximum amount of constitution points character can have.</param>
        /// <returns>Returns the amount of points healed actually.</returns>
        int HealLifePoints(int amount, int max);
        /// <summary>
        /// Heal's character special energy.
        /// </summary>
        /// <param name="amount">Amount of special energy to be healed.</param>
        /// <returns>Return's the actual amount of energy healed.</returns>
        int HealSpecialEnergy(int amount);
        /// <summary>
        /// Get's skill experience.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.Exception"></exception>
        double GetSkillExperience(int skillID);
        /// <summary>
        /// Get's previous skill experience.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.Exception"></exception>
        double GetPreviousSkillExperience(int skillID);
        /// <summary>
        /// Heal's character prayer points by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal prayerpoints.</param>
        /// <returns>Returns the amount of points healed actually.</returns>
        int HealPrayerPoints(int amount);
        /// <summary>
        /// Heal's character prayer points by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal prayerpoints.</param>
        /// <param name="max">Maximum amount of prayer points character can have.</param>
        /// <returns>Returns the amount of points healed actually.</returns>
        int HealPrayerPoints(int amount, int max);
        /// <summary>
        /// Heals (Increases) specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="max">Maximum level of the skill.</param>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Returns the actual heal amount.</returns>
        int HealSkill(int skillID, int max, int amount);
        /// <summary>
        /// Get's character maximum hitpoints.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetMaximumLifePoints();
        /// <summary>
        /// Refreshe's flashed skills.
        /// </summary>
        void RefreshFlashedSkills();
        /// <summary>
        /// Calculate's character bonuses.
        /// </summary>
        void CalculateBonuses();
        /// <summary>
        /// Flashe's specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <exception cref="System.Exception"></exception>
        void FlashSkill(int skillID);
    }
}
