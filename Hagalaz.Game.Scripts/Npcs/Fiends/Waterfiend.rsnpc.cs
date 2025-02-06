using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Npcs.Fiends
{
    /// <summary>
    /// </summary>
    public class Waterfiend : NpcScriptBase
    {
        /// <summary>
        ///     The style
        /// </summary>
        private AttackType _type;

        /// <summary>
        /// </summary>
        private enum AttackType
        {
            /// <summary>
            ///     The magic
            /// </summary>
            Magic,

            /// <summary>
            ///     The ranged
            /// </summary>
            Ranged
        }

        /// <summary>
        ///     Called when [set target].
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnSetTarget(ICreature target) => _type = RandomStatic.Generator.Next(0, 2) == 1 ? AttackType.Magic : AttackType.Ranged;

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType()
        {
            switch (_type)
            {
                case AttackType.Magic:
                    return AttackBonus.Magic;
                case AttackType.Ranged:
                    return AttackBonus.Ranged;
                default:
                    return base.GetAttackBonusType();
            }
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 8;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle()
        {
            switch (_type)
            {
                case AttackType.Magic:
                    return AttackStyle.MagicNormal;
                case AttackType.Ranged:
                    return AttackStyle.RangedAccurate;
                default:
                    return base.GetAttackStyle();
            }
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            switch (_type)
            {
                case AttackType.Magic:
                {
                    var magicMax = 250;
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

                    var delay = (byte)(30 + deltaX * 5 + deltaY * 5);
                    var projectile = new Projectile(2705);
                    projectile.SetSenderData(Owner, 35, false);
                    projectile.SetReceiverData(target, 35);
                    projectile.SetFlyingProperties(10, delay, 5, 0, false);
                    projectile.Display();

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, ((INpcCombat)Owner.Combat).GetMagicDamage(target, magicMax), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
                            target.QueueGraphic(Graphic.Create(2710, 0, 100));
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.StandardMagic, preDmg, ref soaked);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicDamage(target, magicMax) <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);
                        }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                    break;
                }
                case AttackType.Ranged:
                {
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

                    var delay = (byte)(30 + deltaX * 5 + deltaY * 5);
                    var projectile = new Projectile(2705);
                    projectile.SetSenderData(Owner, 35, false);
                    projectile.SetReceiverData(target, 35);
                    projectile.SetFlyingProperties(10, delay, 5, 0, false);
                    projectile.Display();

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.StandardRange, ((INpcCombat)Owner.Combat).GetRangeDamage(target), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
                            target.QueueGraphic(Graphic.Create(2710, 0, 100));
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.StandardRange, preDmg, ref soaked);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetRangeMaxHit(target) <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);
                        }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                    break;
                }
            }

            _type = RandomStatic.Generator.Next(0, 2) == 1 ? AttackType.Ranged : AttackType.Magic;
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [5361];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}