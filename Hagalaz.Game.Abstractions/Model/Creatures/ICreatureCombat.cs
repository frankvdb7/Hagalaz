using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for a creature's combat-related state and actions, managing everything from target selection to damage calculation.
    /// </summary>
    public interface ICreatureCombat
    {
        /// <summary>
        /// Gets a value indicating whether the creature is currently dead.
        /// </summary>
        bool IsDead { get; }
        /// <summary>
        /// Gets the creature's current combat target.
        /// </summary>
        ICreature? Target { get; }
        /// <summary>
        /// Gets the game tick at which the creature can perform its next combat action.
        /// </summary>
        int DelayTick { get; }
        /// <summary>
        /// Gets the last creature that this creature attacked.
        /// </summary>
        ICreature? LastAttacked { get; }
        /// <summary>
        /// Gets a collection of creatures that have recently attacked this creature.
        /// </summary>
        IEnumerable<ICreatureAttackerInfo> RecentAttackers { get; }
        /// <summary>
        /// Executes a single game tick of combat logic for the creature.
        /// </summary>
        void Tick();
        /// <summary>
        /// Sets the creature's combat target and initiates an attack.
        /// </summary>
        /// <param name="target">The creature to be attacked.</param>
        /// <returns><c>true</c> if the target was successfully set; otherwise, <c>false</c>.</returns>
        bool SetTarget(ICreature target);
        /// <summary>
        /// Cancels the creature's current combat target and stops its attack.
        /// </summary>
        void CancelTarget();
        /// <summary>
        /// A callback method executed when this creature kills its target.
        /// </summary>
        /// <param name="target">The creature that was killed.</param>
        void OnTargetKilled(ICreature target);
        /// <summary>
        /// A callback method executed when this creature is killed by another creature.
        /// </summary>
        /// <param name="killer">The creature that delivered the killing blow.</param>
        void OnKilledBy(ICreature killer);
        /// <summary>
        /// Initiates the death sequence for this creature, playing death animations and sounds.
        /// </summary>
        void OnDeath();
        /// <summary>
        /// Renders the spawning effects for this creature, such as animations or graphics.
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// Determines if this creature can be attacked by a specific attacker.
        /// </summary>
        /// <param name="attacker">The creature attempting to attack.</param>
        /// <returns><c>true</c> if the attack is allowed; otherwise, <c>false</c>.</returns>
        bool CanBeAttackedBy(ICreature attacker);
        /// <summary>
        /// A callback method executed after this creature performs an attack on a target.
        /// </summary>
        /// <param name="target">The creature that was attacked.</param>
        void OnAttackPerformed(ICreature target);
        /// <summary>
        /// Determines if this creature can set a specific creature as its combat target.
        /// </summary>
        /// <param name="target">The potential target creature.</param>
        /// <returns><c>true</c> if the target can be set; otherwise, <c>false</c>.</returns>
        bool CanSetTarget(ICreature target);
        /// <summary>
        /// Determines if this creature's loot can be claimed by a specific killer.
        /// </summary>
        /// <param name="killer">The creature that killed this creature.</param>
        /// <returns><c>true</c> if the killer is eligible to receive loot; otherwise, <c>false</c>.</returns>
        bool CanBeLootedBy(ICreature killer);
        /// <summary>
        /// Checks if the creature is currently engaged in combat.
        /// </summary>
        /// <returns><c>true</c> if the creature is in combat; otherwise, <c>false</c>.</returns>
        bool IsInCombat();
        /// <summary>
        /// Processes a direct attack on this creature, applying damage and effects.
        /// </summary>
        /// <param name="attacker">The creature performing the attack.</param>
        /// <param name="damageType">The type of damage being dealt.</param>
        /// <param name="damage">The amount of damage dealt, or -1 for a miss.</param>
        /// <param name="soaked">A reference parameter that will be set to the amount of damage absorbed by defenses.</param>
        /// <returns>The amount of damage that should be displayed on the hitsplat.</returns>
        int Attack(ICreature attacker, DamageType damageType, int damage, ref int soaked);
        /// <summary>
        /// Processes the client-side effects of an incoming attack, such as animations and protection prayer checks, without applying damage.
        /// </summary>
        /// <param name="attacker">The creature performing the attack.</param>
        /// <param name="damageType">The type of damage being dealt.</param>
        /// <param name="damage">The potential amount of damage, or -1 for a miss.</param>
        /// <param name="delay">The delay in client ticks until the attack hits.</param>
        /// <returns>The amount of damage that will be dealt after a delay.</returns>
        int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay);
        /// <summary>
        /// Executes a complete attack action based on a set of parameters.
        /// </summary>
        /// <param name="attackParams">The parameters defining the attack, including target, damage, and type.</param>
        /// <returns>A task handle representing the asynchronous attack operation, which will yield an <see cref="AttackResult"/>.</returns>
        IRsTaskHandle<AttackResult> PerformAttack(AttackParams attackParams);
        /// <summary>
        /// Gets the creature's effective Attack skill level, including any boosts or drains.
        /// </summary>
        /// <returns>The creature's effective Attack level.</returns>
        int GetAttackLevel();
        /// <summary>
        /// Gets the creature's effective Strength skill level, including any boosts or drains.
        /// </summary>
        /// <returns>The creature's effective Strength level.</returns>
        int GetStrengthLevel();
        /// <summary>
        /// Gets the creature's effective Defence skill level, including any boosts or drains.
        /// </summary>
        /// <returns>The creature's effective Defence level.</returns>
        int GetDefenceLevel();
        /// <summary>
        /// Gets the creature's effective Magic skill level, including any boosts or drains.
        /// </summary>
        /// <returns>The creature's effective Magic level.</returns>
        int GetMagicLevel();
        /// <summary>
        /// Gets the creature's current combat style.
        /// </summary>
        /// <returns>The creature's <see cref="AttackStyle"/>.</returns>
        AttackStyle GetAttackStyle();
        /// <summary>
        /// Gets the creature's defensive bonus against a specific type of attack.
        /// </summary>
        /// <param name="attackBonusType">The attack bonus type of the incoming attack.</param>
        /// <returns>The creature's corresponding defensive bonus value.</returns>
        int GetDefenceBonus(AttackBonus attackBonusType);
        /// <summary>
        /// Gets the creature's current attack bonus type (e.g., Stab, Slash, Ranged).
        /// </summary>
        /// <returns>The creature's <see cref="AttackBonus"/> type.</returns>
        AttackBonus GetAttackBonusType();
        /// <summary>
        /// Gets the creature's current attack bonus value for its active combat style.
        /// </summary>
        /// <returns>The creature's attack bonus value.</returns>
        int GetAttackBonus();
        /// <summary>
        /// Gets the value of a specific prayer-based combat bonus.
        /// </summary>
        /// <param name="bonusType">The type of prayer bonus to retrieve.</param>
        /// <returns>The value of the specified prayer bonus.</returns>
        int GetPrayerBonus(BonusPrayerType bonusType);
        /// <summary>
        /// Resets the creature's combat delay, allowing it to attack again immediately if a target is present.
        /// </summary>
        void ResetCombatDelay();
    }
}
