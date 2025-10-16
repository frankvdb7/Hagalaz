using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a player character's combat-specific logic and calculations.
    /// </summary>
    public interface ICharacterCombat
    {
        /// <summary>
        /// Adds Magic and Constitution experience based on the damage dealt.
        /// </summary>
        /// <param name="damage">The amount of damage dealt.</param>
        void AddMagicExperience(int damage);
        /// <summary>
        /// Adds combat experience (e.g., Attack, Strength, Defence, Constitution) based on the damage dealt in melee.
        /// </summary>
        /// <param name="hit">The amount of damage dealt.</param>
        void AddMeleeExperience(int hit);
        /// <summary>
        /// Adds Ranged and Constitution experience based on the damage dealt.
        /// </summary>
        /// <param name="hit">The amount of damage dealt.</param>
        void AddRangedExperience(int hit);
        /// <summary>
        /// Processes a direct attack on this character.
        /// </summary>
        /// <param name="attacker">The creature performing the attack.</param>
        /// <param name="damageType">The type of damage being dealt.</param>
        /// <param name="damage">The amount of damage dealt.</param>
        /// <param name="damageSoaked">A reference parameter that will be set to the amount of damage absorbed.</param>
        /// <returns>The final damage value to be displayed.</returns>
        int Attack(ICreature attacker, DamageType damageType, int damage, ref int damageSoaked);
        /// <summary>
        /// Determines if this character can attack a specific target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns><c>true</c> if the attack is possible; otherwise, <c>false</c>.</returns>
        bool CanAttack(ICreature target);
        /// <summary>
        /// Determines if this character can be attacked by a specific creature.
        /// </summary>
        /// <param name="attacker">The potential attacker.</param>
        /// <returns><c>true</c> if the attack is allowed; otherwise, <c>false</c>.</returns>
        bool CanBeAttackedBy(ICreature attacker);
        /// <summary>
        /// Determines if this character's loot can be claimed by a specific killer.
        /// </summary>
        /// <param name="killer">The creature that killed this character.</param>
        /// <returns><c>true</c> if the killer can receive loot; otherwise, <c>false</c>.</returns>
        bool CanBeLootedBy(ICreature killer);
        /// <summary>
        /// Cancels the character's current combat target.
        /// </summary>
        void CancelTarget();
        /// <summary>
        /// Determines if this character can set a specific creature as its combat target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns><c>true</c> if the target can be set; otherwise, <c>false</c>.</returns>
        bool CanSetTarget(ICreature target);
        /// <summary>
        /// Checks and applies skulling conditions if the character attacks another player.
        /// </summary>
        /// <param name="target">The player being attacked.</param>
        void CheckSkullConditions(ICreature target);
        /// <summary>
        /// Gets the character's current attack bonus type (e.g., Stab, Slash, Magic).
        /// </summary>
        /// <returns>The character's <see cref="AttackBonus"/> type.</returns>
        AttackBonus GetAttackBonusType();
        /// <summary>
        /// Gets the character's attack distance for their current combat style.
        /// </summary>
        /// <returns>The attack distance in tiles.</returns>
        int GetAttackDistance();
        /// <summary>
        /// Gets the character's effective Attack skill level.
        /// </summary>
        /// <returns>The effective Attack level.</returns>
        int GetAttackLevel();
        /// <summary>
        /// Gets the character's attack speed (delay between attacks) in game ticks.
        /// </summary>
        /// <returns>The attack speed.</returns>
        int GetAttackSpeed();
        /// <summary>
        /// Gets the character's current attack style.
        /// </summary>
        /// <returns>The character's <see cref="AttackStyle"/>.</returns>
        AttackStyle GetAttackStyle();
        /// <summary>
        /// Gets the value of a specific equipment-based combat bonus.
        /// </summary>
        /// <param name="bonusType">The type of bonus to retrieve.</param>
        /// <returns>The bonus value.</returns>
        int GetBonus(BonusType bonusType);
        /// <summary>
        /// Gets the combat spell that the character is currently autocasting.
        /// </summary>
        /// <returns>The <see cref="ICombatSpell"/> if one is being autocast; otherwise, <c>null</c>.</returns>
        ICombatSpell? GetCastedSpell();
        /// <summary>
        /// Gets the character's effective Defence skill level.
        /// </summary>
        /// <returns>The effective Defence level.</returns>
        int GetDefenceLevel();
        /// <summary>
        /// Calculates the magic damage for an attack against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="baseDamage">The base damage of the spell.</param>
        /// <returns>The calculated magic damage.</returns>
        int GetMagicDamage(ICreature target, int baseDamage);
        /// <summary>
        /// Gets the character's effective Magic skill level.
        /// </summary>
        /// <returns>The effective Magic level.</returns>
        int GetMagicLevel();
        /// <summary>
        /// Calculates the maximum possible magic hit against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="baseDamage">The base damage of the spell.</param>
        /// <returns>The maximum magic hit.</returns>
        int GetMagicMaxHit(ICreature target, int baseDamage);
        /// <summary>
        /// Calculates the melee damage for an attack against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="specialAttack">A value indicating whether a special attack is being used.</param>
        /// <returns>The calculated melee damage.</returns>
        int GetMeleeDamage(ICreature target, bool specialAttack);
        /// <summary>
        /// Calculates the maximum possible melee hit against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="usingSpecial">A value indicating whether a special attack is being used.</param>
        /// <returns>The maximum melee hit.</returns>
        int GetMeleeMaxHit(ICreature target, bool usingSpecial);
        /// <summary>
        /// Gets the value of a specific prayer-based combat bonus.
        /// </summary>
        /// <param name="bonusType">The type of prayer bonus to retrieve.</param>
        /// <returns>The bonus value.</returns>
        int GetPrayerBonus(BonusPrayerType bonusType);
        /// <summary>
        /// Calculates the ranged damage for an attack against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="specialAttack">A value indicating whether a special attack is being used.</param>
        /// <returns>The calculated ranged damage.</returns>
        int GetRangedDamage(ICreature target, bool specialAttack);
        /// <summary>
        /// Gets the character's effective Ranged skill level.
        /// </summary>
        /// <returns>The effective Ranged level.</returns>
        int GetRangedLevel();
        /// <summary>
        /// Calculates the maximum possible ranged hit against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="usingSpecial">A value indicating whether a special attack is being used.</param>
        /// <returns>The maximum ranged hit.</returns>
        int GetRangedMaxHit(ICreature target, bool usingSpecial);
        /// <summary>
        /// Gets the amount of special attack energy required for the currently equipped weapon's special attack.
        /// </summary>
        /// <returns>The required special energy amount.</returns>
        int GetRequiredSpecialEnergyAmount();
        /// <summary>
        /// Gets the character's effective Strength skill level.
        /// </summary>
        /// <returns>The effective Strength level.</returns>
        int GetStrengthLevel();
        /// <summary>
        /// Processes the effects of an incoming attack without applying damage.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="damageType">The type of damage.</param>
        /// <param name="damage">The potential damage amount.</param>
        /// <param name="delay">The delay in ticks until the hit lands.</param>
        /// <returns>The amount of damage that will be dealt.</returns>
        int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay);
        /// <summary>
        /// A callback executed after the character performs an attack.
        /// </summary>
        /// <param name="target">The creature that was attacked.</param>
        void OnAttackPerformed(ICreature target);
        /// <summary>
        /// A callback executed when the character dies.
        /// </summary>
        void OnDeath();
        /// <summary>
        /// A callback executed when the character is killed by another creature.
        /// </summary>
        /// <param name="killer">The creature that dealt the killing blow.</param>
        void OnKilledBy(ICreature killer);
        /// <summary>
        /// A callback executed when the character spawns into the world.
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// A callback executed when the character kills another creature.
        /// </summary>
        /// <param name="target">The creature that was killed.</param>
        void OnTargetKilled(ICreature target);
        /// <summary>
        /// Calculates and applies defensive modifiers (e.g., armor, protection prayers) to an incoming hit.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="damageType">The type of damage.</param>
        /// <param name="damage">The initial damage amount.</param>
        /// <returns>The final damage after defensive calculations.</returns>
        int PerformDefence(ICreature attacker, DamageType damageType, int damage);
        /// <summary>
        /// Performs the Soul Split curse effect, healing the character and damaging the target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="predictedDamage">The predicted damage of the attack that triggered Soul Split.</param>
        void PerformSoulSplit(ICreature target, int predictedDamage);
        /// <summary>
        /// Sets the character's combat target.
        /// </summary>
        /// <param name="target">The creature to target.</param>
        /// <returns><c>true</c> if the target was set successfully; otherwise, <c>false</c>.</returns>
        bool SetTarget(ICreature target);
    }
}