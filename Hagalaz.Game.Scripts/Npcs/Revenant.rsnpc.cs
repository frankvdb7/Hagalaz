using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Npcs
{
    /// <summary>
    /// </summary>
    internal enum Attack
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
    public class Revenant : NpcScriptBase
    {
        /// <summary>
        ///     The attack.
        /// </summary>
        private Attack _attack;

        /// <summary>
        ///     Performs the aggressiveness check.
        /// </summary>
        public override void AggressivenessTick()
        {
        }

        /// <summary>
        ///     Get's if this npc can aggro attack specific character.
        ///     By default this method does check if character is character.
        ///     This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsAggressiveTowards(ICreature creature) => false;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            switch (_attack)
            {
                case Attack.Melee:
                {
                    RenderAttack();
                    var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
                    var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
                    Owner.Combat.PerformAttack(new AttackParams()
                    {
                        Target = target, Damage = damage, MaxDamage = maxDamage, DamageType = DamageType.StandardMelee,
                    });

                    if (RandomStatic.Generator.NextDouble() >= 0.90)
                    {
                        target.Poison(58);
                    }

                    break;
                }
                case Attack.Ranged:
                {
                    RenderAttack();
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

                    var delay = (byte)(20 + deltaX * 5 + deltaY * 5);

                    var projectile = new Projectile(1278); // 1203
                    projectile.SetSenderData(Owner, 35, false);
                    projectile.SetReceiverData(target, 35);
                    projectile.SetFlyingProperties(50, delay, 0, 180, false);
                    projectile.Display();

                    var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 164);
                    dmg = target.Combat.IncomingAttack(Owner, DamageType.StandardRange, dmg, delay);

                    Owner.QueueTask(new RsTask(() =>
                        {
                            var soak = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.StandardRange, dmg, ref soak);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetRangeMaxHit(target) <= damage);
                            if (soak != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                            }

                            target.QueueHitSplat(splat);
                        }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                    break;
                }
                case Attack.Magic:
                {
                    RenderAttack();
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

                    var delay = (byte)(20 + deltaX * 5 + deltaY * 5);

                    var projectile = new Projectile(1278); // 1203
                    projectile.SetSenderData(Owner, 35, false);
                    projectile.SetReceiverData(target, 35);
                    projectile.SetFlyingProperties(50, delay, 0, 180, false);
                    projectile.Display();

                    var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 164);
                    dmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

                    Owner.QueueTask(new RsTask(() =>
                        {
                            var soak = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 164) <= damage);
                            if (soak != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                            }

                            target.QueueHitSplat(splat);
                        }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                    break;
                }
            }

            GenerateAttack();
        }

        /// <summary>
        ///     Generates the attack.
        /// </summary>
        private void GenerateAttack()
        {
            var chances = new double[5];
            chances[0] = 0.75; // melee
            chances[1] = 0.10; // ranged
            chances[2] = 0.10; // magic
            var hitValue = Random.Shared.Next(chances.Sum());
            var runningValue = 0.0;
            for (var i = 0; i < chances.Length; i++)
            {
                runningValue += chances[i];
                if (!(hitValue < runningValue))
                {
                    continue;
                }

                switch (i)
                {
                    case 0:
                        _attack = Attack.Melee;
                        break;
                    case 1:
                        _attack = Attack.Ranged;
                        break;
                    case 2:
                        _attack = Attack.Magic;
                        break;
                }

                return;
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
            switch (_attack)
            {
                case Attack.Melee:
                    return 1;
                case Attack.Ranged:
                case Attack.Magic:
                    return 7;
                default:
                    return base.GetAttackDistance();
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
            switch (_attack)
            {
                case Attack.Melee:
                    return AttackStyle.MeleeAggressive;
                case Attack.Ranged:
                    return AttackStyle.RangedAccurate;
                case Attack.Magic:
                    return AttackStyle.MagicNormal;
                default:
                    return base.GetAttackStyle();
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
            switch (_attack)
            {
                case Attack.Melee:
                    return AttackBonus.Slash;
                case Attack.Ranged:
                    return AttackBonus.Ranged;
                case Attack.Magic:
                    return AttackBonus.Magic;
                default:
                    return base.GetAttackBonusType();
            }
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [13465, 13466, 13467, 13468, 13469, 13470, 13471, 13472, 13473, 13474, 13475, 13476, 13477, 13478, 13479, 13480, 13481
        ];

        /// <summary>
        ///     Get's called when npc is spawned.
        /// </summary>
        public override void OnSpawn() => _attack = (Attack)RandomStatic.Generator.Next(0, 3);

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() => Owner.AddState(new State(StateType.NpcTypeUndead, int.MaxValue));
    }
}