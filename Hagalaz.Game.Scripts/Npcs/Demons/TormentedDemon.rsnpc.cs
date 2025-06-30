using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Npcs.Demons
{
    /// <summary>
    /// </summary>
    internal enum CombatType
    {
        /// <summary>
        ///     The melee
        /// </summary>
        Melee,

        /// <summary>
        ///     The ranged
        /// </summary>
        Ranged,

        /// <summary>
        ///     The magic
        /// </summary>
        Magic
    }

    /// <summary>
    /// </summary>
    [NpcScriptMetaData([8349, 8352, 8355, 8358, 8361, 8364])]
    public class TormentedDemon : NpcScriptBase
    {
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     The received hits.
        /// </summary>
        private readonly Dictionary<DamageType, int> _receivedHits = new();

        /// <summary>
        ///     The defence type.
        /// </summary>
        private CombatType _defenceType = CombatType.Melee;

        /// <summary>
        ///     The attack type.
        /// </summary>
        private CombatType _attackType = CombatType.Melee;

        /// <summary>
        ///     Wether the shield is enabled.
        /// </summary>
        private bool _shieldEnabled = true;

        /// <summary>
        ///     The shield timer.
        /// </summary>
        private int _shieldTick;

        /// <summary>
        ///     The combat style tick.
        /// </summary>
        private int _combatStyleTick;

        /// <summary>
        ///     The weakener.
        /// </summary>
        private ICharacter? _weakener;

        public TormentedDemon(IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     Called when [cancel target].
        /// </summary>
        public override void OnCancelTarget()
        {
            _weakener = null;
            _receivedHits.Clear();
            base.OnCancelTarget();
        }

        /// <summary>
        ///     Get's called when npc is dead.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDeath()
        {
            _weakener = null;
            _receivedHits.Clear();
            _combatStyleTick = 0;
            EnabledShield();
        }

        /// <summary>
        ///     Happens when attacker attack reaches the npc.
        ///     By default this method does nothing and returns the damage provided in parameters.
        /// </summary>
        /// <param name="attacker">Creature which performed attack.</param>
        /// <param name="damageType">Type of the attack.</param>
        /// <param name="damage">Amount of damage inflicted on this character or -1 if it's a miss.</param>
        /// <returns>
        ///     Return's amount of damage remains after defence.
        /// </returns>
        public override int OnAttack(ICreature attacker, DamageType damageType, int damage)
        {
            AccumulateCombatDamage(damageType, damage);
            return damage;
        }

        /// <summary>
        ///     Called when [incomming attack].
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">The damage.</param>
        /// <param name="delay">The delay.</param>
        /// <returns></returns>
        public override int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay)
        {
            if (_defenceType != CombatType.Melee)
            {
                if (attacker is ICharacter character)
                {
                    if (damageType == DamageType.FullMelee || damageType == DamageType.StandardMelee)
                    {
                        var weapon = character.Equipment[EquipmentSlot.Weapon];
                        if (weapon != null)
                        {
                            if (weapon.Id == 6746 || weapon.Id == 2402) // Darklight & Silverlight
                            {
                                if (damage > 0)
                                {
                                    _weakener = character;
                                    DisableShield();
                                }
                            }
                        }
                    }
                }
            }

            var protect = false;
            switch (_defenceType)
            {
                case CombatType.Melee:
                    {
                        if (damageType == DamageType.FullMelee
                            || damageType == DamageType.StandardMelee)
                        {
                            protect = true;
                        }

                        break;
                    }
                case CombatType.Ranged:
                    {
                        if (damageType == DamageType.FullRange
                            || damageType == DamageType.StandardRange)
                        {
                            protect = true;
                        }

                        break;
                    }
                case CombatType.Magic:
                    {
                        if (damageType == DamageType.FullMagic
                            || damageType == DamageType.StandardMagic)
                        {
                            protect = true;
                        }

                        break;
                    }
            }

            if (protect)
            {
                damage = (int)(damage * 0.60); // 40 % damage reduction (Normal PvP reduction)
            }

            if (_shieldEnabled)
            {
                return (int)(damage * 0.25); // 75 % damage reduction
            }

            return base.OnIncomingAttack(attacker, damageType, damage, delay);
        }

        /// <summary>
        ///     Render's defence of this npc.
        /// </summary>
        /// <param name="delay">Delay in client ticks till attack will reach the target.</param>
        public override void RenderDefence(int delay)
        {
            Owner.QueueAnimation(Animation.Create(Owner.Definition.DefenceAnimation, delay));
            if (_shieldEnabled)
            {
                Owner.QueueGraphic(Graphic.Create(1885, delay));
            }
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            switch (_attackType)
            {
                case CombatType.Ranged:
                    {
                        Owner.QueueAnimation(Animation.Create(10919));

                        var combat = (INpcCombat)Owner.Combat;

                        var deltaX = Owner.Location.X - target.Location.X;
                        var deltaY = Owner.Location.Y - target.Location.Y;
                        if (deltaX < 0)
                        {
                            deltaX = -deltaX;
                        }

                        if (deltaY < 0)
                        {
                            deltaY = -deltaY;
                        }

                        var delay = 20 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(1887)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(delay)
                            .WithFromHeight(50)
                            .WithToHeight(35)
                            .WithDelay(10)
                            .WithSlope(10)
                            .Send();

                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = combat.GetRangeDamage(target),
                            MaxDamage = combat.GetRangeMaxHit(target),
                            Delay = delay,
                            DamageType = DamageType.StandardRange,
                            Target = target
                        });
                        break;
                    }
                case CombatType.Magic:
                    {
                        Owner.QueueAnimation(Animation.Create(10918));

                        var deltaX = Owner.Location.X - target.Location.X;
                        var deltaY = Owner.Location.Y - target.Location.Y;
                        if (deltaX < 0)
                        {
                            deltaX = -deltaX;
                        }

                        if (deltaY < 0)
                        {
                            deltaY = -deltaY;
                        }

                        var delay = 20 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(1884)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(delay)
                            .WithFromHeight(50)
                            .WithToHeight(35)
                            .WithDelay(10)
                            .WithSlope(10)
                            .Send();

                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 300),
                            MaxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 300),
                            Delay = delay,
                            DamageType = DamageType.StandardMagic,
                            Target = target
                        });
                        break;
                    }
                default: base.PerformAttack(target); break;
            }
        }

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle()
        {
            switch (_attackType)
            {
                case CombatType.Melee: return AttackStyle.MeleeAccurate;
                case CombatType.Ranged: return AttackStyle.RangedAccurate;
                case CombatType.Magic: return AttackStyle.MagicNormal;
                default: return base.GetAttackStyle();
            }
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance()
        {
            switch (_attackType)
            {
                case CombatType.Melee: return 1;
                case CombatType.Ranged:
                case CombatType.Magic:
                    return 7;
                default: return base.GetAttackDistance();
            }
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType()
        {
            switch (_attackType)
            {
                case CombatType.Melee: return AttackBonus.Slash;
                case CombatType.Ranged: return AttackBonus.Ranged;
                case CombatType.Magic: return AttackBonus.Magic;
                default: return base.GetAttackBonusType();
            }
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Get's if this npc can be poisoned.
        ///     By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can poison; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanPoison() => false;

        /// <summary>
        ///     Tick's npc.
        /// </summary>
        public override void Tick()
        {
            // attack type
            if (Owner.Combat.IsInCombat())
            {
                _combatStyleTick++;
            }

            if (_combatStyleTick == 25) // 15 seconds
            {
                ChangeAttackType();
            }

            // shield
            if (_shieldEnabled)
            {
                return;
            }

            _shieldTick++;
            if (_shieldTick >= 100) // 60 seconds
            {
                EnabledShield();
            }
        }

        /// <summary>
        ///     Accumulates the combat damage.
        /// </summary>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">The damage.</param>
        private void AccumulateCombatDamage(DamageType damageType, int damage)
        {
            var accumulator = damage;
            if (accumulator <= 0)
            {
                accumulator = 25;
            }

            if (!_receivedHits.TryAdd(damageType, damage))
            {
                _receivedHits[damageType] += accumulator;
            }

            if (_receivedHits[damageType] < 500)
            {
                return;
            }

            switch (damageType)
            {
                case DamageType.FullMelee:
                case DamageType.StandardMelee:
                    SetDefenceType(CombatType.Melee);
                    break;
                case DamageType.FullRange:
                case DamageType.StandardRange:
                    SetDefenceType(CombatType.Ranged);
                    break;
                case DamageType.FullMagic:
                case DamageType.StandardMagic:
                    SetDefenceType(CombatType.Magic);
                    break;
            }

            _receivedHits.Clear();
        }

        /// <summary>
        ///     Sets the type of the defence.
        /// </summary>
        /// <param name="type">The type.</param>
        private void SetDefenceType(CombatType type)
        {
            switch (type)
            {
                case CombatType.Melee: Owner.Appearance.Transform(Owner.Definition.Id); break;
                case CombatType.Ranged: Owner.Appearance.Transform((short)(Owner.Definition.Id + 2)); break;
                case CombatType.Magic: Owner.Appearance.Transform((short)(Owner.Definition.Id + 1)); break;
            }

            _defenceType = type;
        }

        /// <summary>
        ///     Changes the type of the attack.
        /// </summary>
        private void ChangeAttackType()
        {
            Owner.Movement.ClearQueue();
            Owner.QueueAnimation(Animation.Create(10917)); // 'roar'
            Owner.Combat.ResetCombatDelay();

            switch (_attackType)
            {
                case CombatType.Melee:
                    {
                        var rnd = RandomStatic.Generator.Next(0, 2);
                        _attackType = rnd == 1 ? CombatType.Ranged : CombatType.Magic;
                        break;
                    }
                case CombatType.Ranged:
                    {
                        var rnd = RandomStatic.Generator.Next(0, 2);
                        _attackType = rnd == 1 ? CombatType.Melee : CombatType.Magic;
                        break;
                    }
                case CombatType.Magic:
                    {
                        var rnd = RandomStatic.Generator.Next(0, 2);
                        _attackType = rnd == 1 ? CombatType.Melee : CombatType.Ranged;
                        break;
                    }
            }

            _combatStyleTick = 0;
        }

        /// <summary>
        ///     Disables the shield.
        /// </summary>
        private void DisableShield()
        {
            if (_weakener != null && !_weakener.IsDestroyed)
            {
                _weakener.SendChatMessage("The demon is temporarily weakened by your weapon.");
            }

            _shieldTick = 0;
            _shieldEnabled = false;
        }

        /// <summary>
        ///     Enableds the shield.
        /// </summary>
        private void EnabledShield()
        {
            if (_weakener != null && !_weakener.IsDestroyed)
            {
                _weakener.SendChatMessage("The Tormented demon regains its strength against your weapon.");
            }
            else
            {
                _weakener = null;
            }

            _shieldTick = 0;
            _shieldEnabled = true;
        }
    }
}