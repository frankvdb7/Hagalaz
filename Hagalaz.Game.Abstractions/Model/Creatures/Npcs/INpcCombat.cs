using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for an NPC's combat-related logic and calculations.
    /// </summary>
    public interface INpcCombat
    {
        /// <summary>
        /// Processes a direct attack on this NPC.
        /// </summary>
        /// <param name="attacker">The creature performing the attack.</param>
        /// <param name="damageType">The type of damage being dealt.</param>
        /// <param name="damage">The amount of damage dealt.</param>
        /// <param name="damageSoaked">A reference parameter that will be set to the amount of damage absorbed.</param>
        /// <returns>The final damage value to be displayed.</returns>
        int Attack(ICreature attacker, DamageType damageType, int damage, ref int damageSoaked);

        /// <summary>
        /// Determines if this NPC can attack a specific target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns><c>true</c> if the attack is possible; otherwise, <c>false</c>.</returns>
        bool CanAttack(ICreature target);

        /// <summary>
        /// Determines if this NPC can be attacked by a specific creature.
        /// </summary>
        /// <param name="attacker">The potential attacker.</param>
        /// <returns><c>true</c> if the attack is allowed; otherwise, <c>false</c>.</returns>
        bool CanBeAttackedBy(ICreature attacker);

        /// <summary>
        /// Determines if this NPC's loot can be claimed by a specific killer.
        /// </summary>
        /// <param name="killer">The creature that killed this NPC.</param>
        /// <returns><c>true</c> if the killer can receive loot; otherwise, <c>false</c>.</returns>
        bool CanBeLootedBy(ICreature killer);

        /// <summary>
        /// Cancels the NPC's current combat target.
        /// </summary>
        void CancelTarget();

        /// <summary>
        /// Determines if this NPC can set a specific creature as its combat target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns><c>true</c> if the target can be set; otherwise, <c>false</c>.</returns>
        bool CanSetTarget(ICreature target);

        /// <summary>
        /// Gets the NPC's current attack bonus type (e.g., Stab, Slash, Magic).
        /// </summary>
        /// <returns>The NPC's <see cref="AttackBonus"/> type.</returns>
        AttackBonus GetAttackBonusType();

        /// <summary>
        /// Gets the NPC's attack distance for its current combat style.
        /// </summary>
        /// <returns>The attack distance in tiles.</returns>
        int GetAttackDistance();

        /// <summary>
        /// Gets the NPC's effective Attack skill level.
        /// </summary>
        /// <returns>The effective Attack level.</returns>
        int GetAttackLevel();

        /// <summary>
        /// Gets the NPC's attack speed (delay between attacks) in game ticks.
        /// </summary>
        /// <returns>The attack speed.</returns>
        int GetAttackSpeed();

        /// <summary>
        /// Gets the NPC's current attack style.
        /// </summary>
        /// <returns>The NPC's <see cref="AttackStyle"/>.</returns>
        AttackStyle GetAttackStyle();

        /// <summary>
        /// Gets the value of a specific equipment-based combat bonus.
        /// </summary>
        /// <param name="bonusType">The type of bonus to retrieve.</param>
        /// <returns>The bonus value.</returns>
        int GetBonus(BonusType bonusType);

        /// <summary>
        /// Gets the NPC's effective Defence skill level.
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
        /// Gets the NPC's effective Magic skill level.
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
        /// <returns>The calculated melee damage.</returns>
        int GetMeleeDamage(ICreature target);

        /// <summary>
        /// Calculates the melee damage for an attack against a specific target, with a specified maximum damage.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <param name="maxDamage">The maximum damage to inflict.</param>
        /// <returns>The calculated melee damage.</returns>
        int GetMeleeDamage(ICreature target, int maxDamage);

        /// <summary>
        /// Calculates the maximum possible melee hit against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <returns>The maximum melee hit.</returns>
        int GetMeleeMaxHit(ICreature target);

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
        /// <returns>The calculated ranged damage.</returns>
        int GetRangeDamage(ICreature target);

        /// <summary>
        /// Gets the NPC's effective Ranged skill level.
        /// </summary>
        /// <returns>The effective Ranged level.</returns>
        int GetRangedLevel();

        /// <summary>
        /// Calculates the maximum possible ranged hit against a specific target.
        /// </summary>
        /// <param name="target">The target of the attack.</param>
        /// <returns>The maximum ranged hit.</returns>
        int GetRangeMaxHit(ICreature target);

        /// <summary>
        /// Gets the NPC's effective Strength skill level.
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
        int IncomingAttack(ICreature attacker, DamageType damageType, int damage, byte delay);

        /// <summary>
        /// A callback executed after the NPC performs an attack.
        /// </summary>
        /// <param name="target">The creature that was attacked.</param>
        void OnAttackPerformed(ICreature target);

        /// <summary>
        /// A callback executed when the NPC dies.
        /// </summary>
        void OnDeath();

        /// <summary>
        /// A callback executed when the NPC is killed by another creature.
        /// </summary>
        /// <param name="killer">The creature that dealt the killing blow.</param>
        void OnKilledBy(ICreature killer);

        /// <summary>
        /// A callback executed when the NPC spawns into the world.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// A callback executed when the NPC kills another creature.
        /// </summary>
        /// <param name="target">The creature that was killed.</param>
        void OnTargetKilled(ICreature target);

        /// <summary>
        /// Sets the NPC's combat target.
        /// </summary>
        /// <param name="target">The creature to target.</param>
        /// <returns><c>true</c> if the target was set successfully; otherwise, <c>false</c>.</returns>
        bool SetTarget(ICreature target);

        /// <summary>
        /// Executes an attack using the provided parameters.
        /// </summary>
        /// <param name="attackParams">The parameters that define the attack, including target, damage, and damage type.</param>
        /// <returns>An asynchronous task handle containing the result of the attack.</returns>
        IRsTaskHandle<AttackResult> PerformAttack(AttackParams attackParams);
    }
}