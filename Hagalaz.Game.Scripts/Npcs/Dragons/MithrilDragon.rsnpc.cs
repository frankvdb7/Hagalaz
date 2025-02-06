using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Npcs.Dragons
{
    /// <summary>
    /// </summary>
    public class MithrilDragon : StandardDragon
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            Bonus = AttackBonus.Magic;
            Style = AttackStyle.MagicNormal;
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance()
        {
            if (Bonus == AttackBonus.Ranged || Owner.Combat.Target != null && !Owner.WithinRange(Owner.Combat.Target, 1))
            {
                return 8;
            }

            return 1;
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            if (!Owner.WithinRange(target, 1))
            {
                if (RandomStatic.Generator.Next(0, 100) >= 51)
                {
                    Bonus = AttackBonus.Ranged;
                    Style = AttackStyle.RangedAccurate;
                    Owner.QueueAnimation(Animation.Create(14252));

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
                    var projectile = new Projectile(12);
                    projectile.SetSenderData(Owner, 50, false);
                    projectile.SetReceiverData(target, 35);
                    projectile.SetFlyingProperties(10, delay, 10, 0, false);
                    projectile.Display();

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.StandardRange, ((INpcCombat)Owner.Combat).GetRangeDamage(target), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
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
                }
                else
                {
                    Bonus = AttackBonus.Magic;
                    Style = AttackStyle.MagicNormal;
                    Owner.QueueAnimation(Animation.Create(14252));

                    const int magicMax = 550;
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
                    var projectile = new Projectile(2705);
                    projectile.SetSenderData(Owner, 50, false);
                    projectile.SetReceiverData(target, 35);
                    projectile.SetFlyingProperties(10, delay, 10, 0, false);
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
                }
            }
            else
            {
                if (RandomStatic.Generator.Next(0, 100) >= 80)
                {
                    Bonus = AttackBonus.Magic;
                    Style = AttackStyle.MagicNormal;

                    const int maxHit = 550;
                    // Dragon fire
                    Owner.QueueAnimation(Animation.Create(13164));
                    Owner.QueueGraphic(Graphic.Create(2465));

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.DragonFire, ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
                            target.QueueGraphic(Graphic.Create(439));
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.DragonFire, preDmg, ref soaked);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, maxHit <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);
                        }, 2));
                }
                else
                {
                    Style = AttackStyle.MeleeAggressive;
                    Bonus = AttackBonus.Slash;
                    RenderAttack();
                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMelee, ((INpcCombat)Owner.Combat).GetMeleeDamage(target), 0);
                    var soaked = -1;
                    var damage = target.Combat.Attack(Owner, DamageType.StandardMelee, preDmg, ref soaked);
                    var splat = new HitSplat(Owner);
                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target) <= damage);
                    if (soaked != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                    }

                    target.QueueHitSplat(splat);
                }
            }
        }

        /// <summary>
        ///     Get's npcs suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableNpcs() => [5363];
    }
}