using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreatureCombat
    {
        /// <summary>
        /// Contains boolean if specific creature is dead.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is dead; otherwise, <c>false</c>.
        /// </value>
        bool IsDead { get; }
        /// <summary>
        ///     Contains target character or null.
        /// </summary>
        /// <value>The target.</value>
        ICreature? Target { get; }
        /// <summary>
        ///     Contains combat delay tick.
        /// </summary>
        /// <value>The delay tick.</value>
        int DelayTick { get; }
        /// <summary>
        /// Contains last target or null.
        /// </summary>
        /// <value>
        /// The last attacked.
        /// </value>
        ICreature? LastAttacked { get; }
        /// <summary>
        /// Gets the recent attackers.
        /// </summary>
        /// <value>
        /// The recent attackers.
        /// </value>
        IEnumerable<ICreatureAttackerInfo> RecentAttackers { get; }
        /// <summary>
        /// Ticks this instance.
        /// </summary>
        void Tick();
        /// <summary>
        /// Set's ( Attacks ) the specified target ('target')
        /// </summary>
        /// <param name="target">Creature which should be attacked.</param>
        /// <returns>
        /// If creature target was set successfully.
        /// </returns>
        bool SetTarget(ICreature target);
        /// <summary>
        ///     Cancel's current target.
        /// </summary>
        void CancelTarget();
        /// <summary>
        ///     This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        void OnTargetKilled(ICreature target);
        /// <summary>
        ///     This method gets executed on creatures death by creature.
        /// </summary>
        /// <param name="killer">The creature.</param>
        void OnKilledBy(ICreature killer);
        /// <summary>
        ///     Render's death of this creature.
        /// </summary>
        void OnDeath();
        /// <summary>
        ///     Render's spawning of this character.
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// Get's if this creature can be attacked by specified attacker.
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can be attacked by] the specified attacker; otherwise, <c>false</c>.
        /// </returns>
        bool CanBeAttackedBy(ICreature attacker);
        /// <summary>
        ///     Get's called after attack was performed to specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        void OnAttackPerformed(ICreature target);
        /// <summary>
        ///     Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> if this instance [can set target] the specified target; otherwise, <c>false</c>.</returns>
        bool CanSetTarget(ICreature target);
        /// <summary>
        ///     Determines whether this instance [can be looted].
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns><c>true</c> if this instance [can be looted]; otherwise, <c>false</c>.</returns>
        bool CanBeLootedBy(ICreature killer);
        /// <summary>
        /// Get's if this creature is in combat.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is in combat]; otherwise, <c>false</c>.
        /// </returns>
        bool IsInCombat();
        /// <summary>
        /// Perform's attack on this creature.
        /// Attacker target must be this creature.
        /// Returns amount of damage that should be rendered on hitsplat.
        /// If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 in case of miss.</param>
        /// <param name="soaked">Variable which will be set to amount of damage that was soaked.
        /// If no damage was soaked then the variable will be -1.</param>
        /// <returns>
        /// Returns amount of damage that should be rendered on hitsplat.
        /// </returns>
        int Attack(ICreature attacker, DamageType damageType, int damage, ref int soaked);
        /// <summary>
        /// Performs incoming attack on this creature.
        /// This attack does not damage the target but does check for protection prayers and performs
        /// animations.
        /// Attacker target must be this creature.
        /// If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker creature.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 in case of miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach target.</param>
        /// <returns>
        /// Returns amount of damage that should be dealt.
        /// </returns>
        int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay);
        /// <summary>
        ///     Performs the attack.
        /// </summary>
        IRsTaskHandle<AttackResult> PerformAttack(AttackParams attackParams);
        /// <summary>
        /// Get's attack level of this creature.
        /// </summary>
        /// <returns>
        /// System.Int32.
        /// </returns>
        int GetAttackLevel();
        /// <summary>
        /// Get's strength level of this creature.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetStrengthLevel();
        /// <summary>
        /// Get's defence level of this creature.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetDefenceLevel();
        /// <summary>
        /// Get's magic level .
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetMagicLevel();
        /// <summary>
        /// Get's attack style of this creature.
        /// </summary>
        /// <returns>AttackStyle.</returns>
        AttackStyle GetAttackStyle();
        /// <summary>
        /// Get's this creature defence bonus.
        /// </summary>
        /// <param name="attackBonusType">Type of the attackers attack bonus.</param>
        /// <returns>System.Int32.</returns>
        int GetDefenceBonus(AttackBonus attackBonusType);
        /// <summary>
        /// Get's attack bonus of this creature.
        /// </summary>
        /// <returns>AttackBonus.</returns>
        AttackBonus GetAttackBonusType();
        /// <summary>
        /// Get's current attack bonus.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetAttackBonus();
        /// <summary>
        /// Gets the prayer bonus.
        /// </summary>
        /// <param name="bonusType">Type of the bonus.</param>
        /// <returns></returns>
        int GetPrayerBonus(BonusPrayerType bonusType);
        /// <summary>
        ///     Resets the combat delay.
        /// </summary>
        void ResetCombatDelay();
    }
}
