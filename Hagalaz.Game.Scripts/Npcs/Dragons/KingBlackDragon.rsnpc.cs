using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Features.States;
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

namespace Hagalaz.Game.Scripts.Npcs.Dragons
{
    /// <summary>
    /// </summary>
    internal enum Attacks
    {
        /// <summary>
        ///     The normal.
        /// </summary>
        Normal,

        /// <summary>
        ///     The normal dragon fire
        /// </summary>
        NormalDragonFire,

        /// <summary>
        ///     The shock breath
        /// </summary>
        ShockingBreath,

        /// <summary>
        ///     The ice breath
        /// </summary>
        FreezingBreath,

        /// <summary>
        ///     The poison breath
        /// </summary>
        PoisonousBreath
    }

    /// <summary>
    ///     Contains the King Black Dragon script.
    /// </summary>
    public class KingBlackDragon : NpcScriptBase
    {
        /// <summary>
        ///     The attack type.
        /// </summary>
        private Attacks _attackType = Attacks.Normal;

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() => Owner.AddState(new State(StateType.NpcTypeDragon, int.MaxValue));

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            _attackType = GenerateAttackType(target);
            switch (_attackType)
            {
                case Attacks.Normal:
                {
                    Owner.QueueAnimation(Animation.Create(Owner.Definition.AttackAnimation));
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
                    break;
                }
                case Attacks.NormalDragonFire:
                {
                    var maxHit = 650;
                    Owner.QueueAnimation(Animation.Create(17786));
                    Owner.QueueGraphic(Graphic.Create(3441));

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

                    var duration = (byte)(50 + deltaX * 2 + deltaY * 2);

                    var projectile = new Projectile(3442);
                    projectile.SetSenderData(Owner, 60, false);
                    projectile.SetReceiverData(target, 41);
                    projectile.SetFlyingProperties(50, duration, 0, 0, false);
                    projectile.Display();

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.DragonFire, ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
                            target.QueueGraphic(Graphic.Create(3443));
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.DragonFire, preDmg, ref soaked);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, maxHit <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);
                        }, CreatureHelper.CalculateTicksForClientTicks(duration)));
                    break;
                }
                case Attacks.PoisonousBreath:
                {
                    const int maxHit = 650;
                    Owner.QueueAnimation(Animation.Create(17785));
                    Owner.QueueGraphic(Graphic.Create(3435));

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

                    var duration = (byte)(50 + deltaX * 2 + deltaY * 2);

                    var projectile = new Projectile(3436);
                    projectile.SetSenderData(Owner, 60, false);
                    projectile.SetReceiverData(target, 41);
                    projectile.SetFlyingProperties(25, duration, 0, 0, false);
                    projectile.Display();

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.DragonFire, ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
                            target.QueueGraphic(Graphic.Create(3437));
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.DragonFire, preDmg, ref soaked);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, maxHit <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);

                            if (damage != -1)
                            {
                                target.Poison(80);
                            }
                        }, CreatureHelper.CalculateTicksForClientTicks(duration)));
                    break;
                }
                case Attacks.ShockingBreath:
                {
                    const int maxHit = 650;
                    Owner.QueueAnimation(Animation.Create(17784));
                    Owner.QueueGraphic(Graphic.Create(3432));

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

                    var duration = (byte)(50 + deltaX * 2 + deltaY * 2);

                    var projectile = new Projectile(3433);
                    projectile.SetSenderData(Owner, 60, false);
                    projectile.SetReceiverData(target, 41);
                    projectile.SetFlyingProperties(30, duration, 0, 0, false);
                    projectile.Display();

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.DragonFire, ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
                            target.QueueGraphic(Graphic.Create(3434));
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.DragonFire, preDmg, ref soaked);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, maxHit <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);

                            if (damage != -1)
                            {
                                // TODO - Reduce stats
                            }
                        }, CreatureHelper.CalculateTicksForClientTicks(duration)));
                    break;
                }
                case Attacks.FreezingBreath:
                {
                    const int maxHit = 650;
                    Owner.QueueAnimation(Animation.Create(17783));
                    Owner.QueueGraphic(Graphic.Create(3438));

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

                    var duration = (byte)(50 + deltaX * 2 + deltaY * 2);

                    var projectile = new Projectile(3439);
                    projectile.SetSenderData(Owner, 60, false);
                    projectile.SetReceiverData(target, 41);
                    projectile.SetFlyingProperties(25, duration, 0, 0, false);
                    projectile.Display();

                    var preDmg = target.Combat.IncomingAttack(Owner, DamageType.DragonFire, ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit), 0);
                    Owner.QueueTask(new RsTask(() =>
                        {
                            target.QueueGraphic(Graphic.Create(3440));
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.DragonFire, preDmg, ref soaked);
                            var splat = new HitSplat(Owner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, maxHit <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);

                            if (damage != -1)
                            {
                                if (0.50 >= RandomStatic.Generator.NextDouble())
                                {
                                    target.Freeze(8, 8);
                                }
                                else
                                {
                                    target.Combat.ResetCombatDelay();
                                }
                            }
                        }, CreatureHelper.CalculateTicksForClientTicks(duration)));
                    break;
                }
            }
        }

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        /// <param name="target">The target.</param>
        private static Attacks GenerateAttackType(ICreature target)
        {
            var chances = new double[5];
            chances[0] = 0.75; // normal attack
            chances[1] = 0.25; // normal dragonfire
            chances[2] = 0.05; // poison breath
            chances[3] = 0.05; // freezing breath
            chances[4] = 0.05; // shocking breath
            var hitValue = Random.Shared.Next(chances.Sum());
            var runningValue = 0.0;
            for (var i = 0; i < chances.Length; i++)
            {
                runningValue += chances[i];
                if (hitValue < runningValue)
                {
                    if (i == 0)
                    {
                        return Attacks.Normal;
                    }

                    if (i == 1)
                    {
                        return Attacks.NormalDragonFire;
                    }

                    if (i == 2)
                    {
                        return Attacks.PoisonousBreath;
                    }

                    if (i == 3)
                    {
                        return Attacks.FreezingBreath;
                    }

                    if (i == 4)
                    {
                        return Attacks.ShockingBreath;
                    }
                }
            }

            return Attacks.Normal;
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
        public override bool IsAggressiveTowards(ICreature creature)
        {
            if (creature.IsDestroyed)
            {
                return false;
            }

            if (Owner.Area == creature.Area)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => 1;

        /// <summary>
        ///     Get's attack speed of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => Owner.Definition.AttackSpeed;

        /// <summary>
        ///     Get's attack style of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle()
        {
            if (_attackType == Attacks.Normal)
            {
                return AttackStyle.MeleeAggressive;
            }

            return AttackStyle.MagicNormal;
        }

        /// <summary>
        ///     Get's attack bonus of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType()
        {
            if (_attackType == Attacks.Normal)
            {
                return AttackBonus.Slash;
            }

            return AttackBonus.Magic;
        }

        /// <summary>
        ///     Get's npcs suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableNpcs() => [50];
    }
}