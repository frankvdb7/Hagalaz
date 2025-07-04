using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    public interface INpcCombat
    {
        int Attack(ICreature attacker, DamageType damageType, int damage, ref int damageSoaked);
        bool CanAttack(ICreature target);
        bool CanBeAttackedBy(ICreature attacker);
        bool CanBeLootedBy(ICreature killer);
        void CancelTarget();
        bool CanSetTarget(ICreature target);
        AttackBonus GetAttackBonusType();
        int GetAttackDistance();
        int GetAttackLevel();
        int GetAttackSpeed();
        AttackStyle GetAttackStyle();
        int GetBonus(BonusType bonusType);
        int GetDefenceLevel();
        int GetMagicDamage(ICreature target, int baseDamage);
        int GetMagicLevel();
        int GetMagicMaxHit(ICreature target, int baseDamage);
        int GetMeleeDamage(ICreature target);
        int GetMeleeDamage(ICreature target, int maxDamage);
        int GetMeleeMaxHit(ICreature target);
        int GetPrayerBonus(BonusPrayerType bonusType);
        int GetRangeDamage(ICreature target);
        int GetRangedLevel();
        int GetRangeMaxHit(ICreature target);
        int GetStrengthLevel();
        int IncomingAttack(ICreature attacker, DamageType damageType, int damage, byte delay);
        void OnAttackPerformed(ICreature target);
        void OnDeath();
        void OnKilledBy(ICreature killer);
        void OnSpawn();
        void OnTargetKilled(ICreature target);
        bool SetTarget(ICreature target);
        /// <summary>
        /// Executes an attack using the provided parameters.
        /// </summary>
        /// <param name="attackParams">The parameters that define the attack, including target, damage, and damage type.</param>
        /// <returns>An asynchronous task handle containing the result of the attack.</returns>
        IRsTaskHandle<AttackResult> PerformAttack(AttackParams attackParams);
    }
}