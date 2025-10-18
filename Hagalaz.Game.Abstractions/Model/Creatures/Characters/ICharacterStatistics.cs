using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a player character's statistics, such as skills, combat levels, and energy sources.
    /// </summary>
    /// <seealso cref="Hagalaz.Game.Abstractions.Model.Creatures.ICreatureStatistics" />
    public interface ICharacterStatistics : ICreatureStatistics
    {
        /// <summary>
        /// Gets the character's current prayer points.
        /// </summary>
        int PrayerPoints { get; }

        /// <summary>
        /// Gets the character's current special attack energy.
        /// </summary>
        int SpecialEnergy { get; }

        /// <summary>
        /// Gets the character's current run energy.
        /// </summary>
        int RunEnergy { get; }

        /// <summary>
        /// Gets the character's combat level, excluding the Summoning skill.
        /// </summary>
        int BaseCombatLevel { get; }

        /// <summary>
        /// Gets the character's combat level, including the Summoning skill.
        /// </summary>
        int FullCombatLevel { get; }

        /// <summary>
        /// Gets the character's total level across all skills.
        /// </summary>
        int TotalLevel { get; }

        /// <summary>
        /// Gets the total time the character has been played.
        /// </summary>
        TimeSpan PlayTime { get; }

        /// <summary>
        /// Sets the experience value for a specific XP counter slot.
        /// </summary>
        /// <param name="counterID">The ID of the counter slot (0-2).</param>
        /// <param name="xp">The experience value to set.</param>
        void SetXpCounter(int counterID, double xp);

        /// <summary>
        /// Sets a specific XP counter to track the experience gains of a particular skill.
        /// </summary>
        /// <param name="counterID">The ID of the counter slot to configure.</param>
        /// <param name="skillID">The ID of the skill to be tracked.</param>
        void SetTrackedXpCounter(int counterID, int skillID);

        /// <summary>
        /// Sends an update to the client to refresh the special energy display.
        /// </summary>
        void RefreshSpecialEnergy();

        /// <summary>
        /// Sends an update to the client to refresh all XP counter displays.
        /// </summary>
        void RefreshXpCounters();

        /// <summary>
        /// Sends an update to the client to refresh the life points orb display.
        /// </summary>
        void RefreshLifePoints();

        /// <summary>
        /// Resets any temporarily boosted skill levels back to their base level.
        /// </summary>
        void NormalizeBoostedStatistics();

        /// <summary>
        /// Sends an update to the client to refresh the poison status display.
        /// </summary>
        void RefreshPoison();

        /// <summary>
        /// Sends an update to the client to refresh the prayer points display.
        /// </summary>
        void RefreshPrayerPoints();

        /// <summary>
        /// Sends an update to the client to refresh the run energy display.
        /// </summary>
        void RefreshRunEnergy();

        /// <summary>
        /// Restores the levels of specified skills to their base values.
        /// </summary>
        /// <param name="skillIDs">An array of skill IDs to normalize.</param>
        void NormaliseSkills(int[] skillIDs);

        /// <summary>
        /// Toggles the enabled state of a specific XP counter.
        /// </summary>
        /// <param name="counterID">The ID of the counter to toggle.</param>
        void ToggleXpCounter(int counterID);

        /// <summary>
        /// Adds a specified amount of experience to a skill, applying the server's XP rate multiplier.
        /// </summary>
        /// <param name="skillID">The ID of the skill to add experience to.</param>
        /// <param name="experience">The base amount of experience to add.</param>
        /// <returns><c>true</c> if the character gained a level; otherwise, <c>false</c>.</returns>
        bool AddExperience(int skillID, double experience);

        /// <summary>
        /// Sets the current level of a specific skill.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <param name="level">The new level for the skill.</param>
        void SetSkillLevel(int skillID, int level);

        /// <summary>
        /// Sets the total experience of a specific skill.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <param name="experience">The new total experience for the skill.</param>
        void SetSkillExperience(int skillID, double experience);

        /// <summary>
        /// Calculates the skill level for a given amount of experience.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <returns>The corresponding skill level.</returns>
        int LevelForExperience(int skillID);

        /// <summary>
        /// Drains a specified amount of run energy from the character.
        /// </summary>
        /// <param name="amount">The amount of run energy to drain.</param>
        /// <returns>The actual amount of energy that was drained.</returns>
        int DrainRunEnergy(int amount);

        /// <summary>
        /// Drains a specified amount of prayer points from the character.
        /// </summary>
        /// <param name="amount">The amount of prayer points to drain.</param>
        /// <returns>The actual amount of prayer points that were drained.</returns>
        int DrainPrayerPoints(int amount);

        /// <summary>
        /// Gets the character's maximum prayer points, based on their Prayer level.
        /// </summary>
        /// <returns>The maximum prayer points.</returns>
        int GetMaximumPrayerPoints();

        /// <summary>
        /// Drains a specified amount of special attack energy from the character.
        /// </summary>
        /// <param name="amount">The amount of special energy to drain.</param>
        /// <returns>The actual amount of energy that was drained.</returns>
        int DrainSpecialEnergy(int amount);

        /// <summary>
        /// Sets the target level for a specific skill, for use with the skill guide interface.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <param name="level">The target level.</param>
        void SetSkillTargetLevel(int skillID, int level);

        /// <summary>
        /// Sets the target experience for a specific skill, for use with the skill guide interface.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <param name="experience">The target experience.</param>
        void SetSkillTargetExperience(int skillID, int experience);

        /// <summary>
        /// Stops the flashing effect on a skill icon in the stats tab.
        /// </summary>
        /// <param name="skillID">The ID of the skill to stop flashing.</param>
        void StopFlashingSkill(int skillID);

        /// <summary>
        /// Restores a specified amount of run energy to the character.
        /// </summary>
        /// <param name="amount">The amount of run energy to restore.</param>
        /// <returns>The actual amount of energy that was restored.</returns>
        int HealRunEnergy(int amount);

        /// <summary>
        /// Sends an update to the client to refresh all skill level displays.
        /// </summary>
        void RefreshSkills();

        /// <summary>
        /// Checks if a specific skill icon is currently flashing (e.g., due to a level drain).
        /// </summary>
        /// <param name="skillID">The ID of the skill to check.</param>
        /// <returns><c>true</c> if the skill is flashing; otherwise, <c>false</c>.</returns>
        bool SkillFlashed(int skillID);

        /// <summary>
        /// Sends an update to the client to refresh the skill target displays.
        /// </summary>
        void RefreshSkillTargets();

        /// <summary>
        /// Restores a specified amount of life points to the character, up to a given maximum.
        /// </summary>
        /// <param name="amount">The amount of life points to restore.</param>
        /// <param name="max">The maximum life points the character can be healed to.</param>
        /// <returns>The actual amount of life points that were restored.</returns>
        int HealLifePoints(int amount, int max);

        /// <summary>
        /// Restores a specified amount of special attack energy to the character.
        /// </summary>
        /// <param name="amount">The amount of special energy to restore.</param>
        /// <returns>The actual amount of energy that was restored.</returns>
        int HealSpecialEnergy(int amount);

        /// <summary>
        /// Gets the total experience for a specific skill.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <returns>The total experience in the skill.</returns>
        double GetSkillExperience(int skillID);

        /// <summary>
        /// Gets the total experience for a specific skill from the previous game state (before the last update).
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <returns>The previous total experience in the skill.</returns>
        double GetPreviousSkillExperience(int skillID);

        /// <summary>
        /// Restores a specified amount of prayer points to the character.
        /// </summary>
        /// <param name="amount">The amount of prayer points to restore.</param>
        /// <returns>The actual amount of points that were restored.</returns>
        int HealPrayerPoints(int amount);

        /// <summary>
        /// Restores a specified amount of prayer points to the character, up to a given maximum.
        /// </summary>
        /// <param name="amount">The amount of prayer points to restore.</param>
        /// <param name="max">The maximum prayer points the character can be healed to.</param>
        /// <returns>The actual amount of points that were restored.</returns>
        int HealPrayerPoints(int amount, int max);

        /// <summary>
        /// Increases the level of a specific skill, up to a given maximum.
        /// </summary>
        /// <param name="skillID">The ID of the skill to boost.</param>
        /// <param name="max">The maximum level the skill can be boosted to.</param>
        /// <param name="amount">The amount to increase the skill level by.</param>
        /// <returns>The actual amount the skill was boosted.</returns>
        int HealSkill(int skillID, int max, int amount);

        /// <summary>
        /// Gets the character's maximum life points, based on their Constitution level.
        /// </summary>
        /// <returns>The maximum life points.</returns>
        int GetMaximumLifePoints();

        /// <summary>
        /// Sends an update to the client to refresh the display of all flashing skill icons.
        /// </summary>
        void RefreshFlashedSkills();

        /// <summary>
        /// Recalculates all of the character's combat bonuses based on their equipment and active prayers.
        /// </summary>
        void CalculateBonuses();

        /// <summary>
        /// Starts the flashing effect on a skill icon in the stats tab.
        /// </summary>
        /// <param name="skillID">The ID of the skill to flash.</param>
        void FlashSkill(int skillID);
    }
}