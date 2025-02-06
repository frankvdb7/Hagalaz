using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public interface ICharacterCombat
    {
        void AddMagicExperience(int damage);
        void AddMeleeExperience(int hit);
        void AddRangedExperience(int hit);
        int Attack(ICreature attacker, DamageType damageType, int damage, ref int damageSoaked);
        bool CanAttack(ICreature target);
        bool CanBeAttackedBy(ICreature attacker);
        bool CanBeLootedBy(ICreature killer);
        void CancelTarget();
        bool CanSetTarget(ICreature target);
        void CheckSkullConditions(ICreature target);
        AttackBonus GetAttackBonusType();
        int GetAttackDistance();
        int GetAttackLevel();
        int GetAttackSpeed();
        AttackStyle GetAttackStyle();
        int GetBonus(BonusType bonusType);
        ICombatSpell? GetCastedSpell();
        int GetDefenceLevel();
        int GetMagicDamage(ICreature target, int baseDamage);
        int GetMagicLevel();
        int GetMagicMaxHit(ICreature target, int baseDamage);
        int GetMeleeDamage(ICreature target, bool specialAttack);
        int GetMeleeMaxHit(ICreature target, bool usingSpecial);
        int GetPrayerBonus(BonusPrayerType bonusType);
        int GetRangedDamage(ICreature target, bool specialAttack);
        int GetRangedLevel();
        int GetRangedMaxHit(ICreature target, bool usingSpecial);
        int GetRequiredSpecialEnergyAmount();
        int GetStrengthLevel();
        int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay);
        void OnAttackPerformed(ICreature target);
        void OnDeath();
        void OnKilledBy(ICreature killer);
        void OnSpawn();
        void OnTargetKilled(ICreature target);
        int PerformDefence(ICreature attacker, DamageType damageType, int damage);
        void PerformSoulSplit(ICreature target, int predictedDamage);
        bool SetTarget(ICreature target);
    }
}