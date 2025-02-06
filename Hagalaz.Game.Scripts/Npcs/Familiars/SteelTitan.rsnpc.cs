using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    [NpcScriptMetaData([7343, 7344])]
    public class SteelTitan : FamiliarScriptBase
    {
        /// <summary>
        ///     The attack type
        ///     0 = Melee.
        ///     1 = Ranged.
        ///     2 = Magic.
        /// </summary>
        private int _attackType;

        public SteelTitan(ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService) : base(pathFinder, npcService, itemService) { }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void InitializeFamiliar() { }

        /// <summary>
        ///     Performs the special attack.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformSpecialMove(IRuneObject target)
        {
            if (!CheckPerformSpecialMove())
            {
                SetUsingSpecialMove(false);
                return;
            }

            base.PerformSpecialMove(target);

            if (target is not ICreature victim)
            {
                return;
            }

            _attackType = Owner.WithinRange(victim, 1) ? 0 : 1;

            var useMelee = _attackType == 0 && Owner.WithinRange(victim, 1);

            if (useMelee)
            {
                RenderAttack();
            }
            else
            {
                Owner.QueueAnimation(Animation.Create(8196));
            }

            var combat = (INpcCombat)Owner.Combat;

            var damage1 = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim);
            var damage2 = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim);
            var damage3 = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim);
            var damage4 = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim);
            var soak1 = -1;
            var soak2 = -1;
            var soak3 = -1;
            var soak4 = -1;
            damage1 = victim.Combat.Attack(Owner, DamageType.FullSummoning, damage1, ref soak1);
            damage2 = victim.Combat.Attack(Owner, DamageType.FullSummoning, damage2, ref soak2);
            var splat1 = new HitSplat(Owner);
            var splat2 = new HitSplat(Owner);
            splat1.SetFirstSplat(damage1 > 0 ? useMelee ? HitSplatType.HitMeleeDamage : HitSplatType.HitRangeDamage : HitSplatType.HitMiss,
                damage1 > 0 ? damage1 : 0,
                damage1 >= 244);
            splat2.SetFirstSplat(damage2 > 0 ? useMelee ? HitSplatType.HitMeleeDamage : HitSplatType.HitRangeDamage : HitSplatType.HitMiss,
                damage2 > 0 ? damage2 : 0,
                damage2 >= 244);
            if (soak1 != -1)
            {
                splat1.SetSecondSplat(HitSplatType.HitDefendedDamage, soak1, false);
            }

            if (soak2 != -1)
            {
                splat2.SetSecondSplat(HitSplatType.HitDefendedDamage, soak2, false);
            }

            victim.QueueHitSplat(splat1);
            victim.QueueHitSplat(splat2);
            victim.QueueTask(new RsTask(() =>
                {
                    damage3 = victim.Combat.Attack(Owner, DamageType.FullMelee, damage3, ref soak3);
                    damage4 = victim.Combat.Attack(Owner, DamageType.FullMelee, damage4, ref soak4);
                    var splat3 = new HitSplat(Owner);
                    var splat4 = new HitSplat(Owner);
                    splat3.SetFirstSplat(damage3 > 0 ? useMelee ? HitSplatType.HitMeleeDamage : HitSplatType.HitRangeDamage : HitSplatType.HitMiss,
                        damage3 > 0 ? damage3 : 0,
                        damage3 >= 244);
                    splat4.SetFirstSplat(damage4 > 0 ? useMelee ? HitSplatType.HitMeleeDamage : HitSplatType.HitRangeDamage : HitSplatType.HitMiss,
                        damage4 > 0 ? damage4 : 0,
                        damage4 >= 244);
                    if (soak3 != -1)
                    {
                        splat3.SetSecondSplat(HitSplatType.HitDefendedDamage, soak3, false);
                    }

                    if (soak4 != -1)
                    {
                        splat4.SetSecondSplat(HitSplatType.HitDefendedDamage, soak4, false);
                    }

                    victim.QueueHitSplat(splat3);
                    victim.QueueHitSplat(splat4);
                },
                1));
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            if (UsingSpecialMove)
            {
                PerformSpecialMove(target);
            }
            else
            {
                switch (_attackType)
                {
                    case 0:
                        {
                            // Melee attack
                            RenderAttack();
                            var preDmg = target.Combat.IncomingAttack(Owner, DamageType.FullSummoning, ((INpcCombat)Owner.Combat).GetMeleeDamage(target), 0);
                            var soaked = -1;
                            var damage = target.Combat.Attack(Owner, DamageType.FullSummoning, preDmg, ref soaked);
                            var splat = new HitSplat(Summoner);
                            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage,
                                damage == -1 ? 0 : damage,
                                ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target) <= damage);
                            if (soaked != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                            }

                            target.QueueHitSplat(splat);
                            break;
                        }
                    case 1:
                        {
                            // Ranged attack

                            Owner.QueueAnimation(Animation.Create(8190));

                            var delay = (byte)Math.Max(10,
                                (int)Location.GetDistance(Owner.Location.X, Owner.Location.Y, target.Location.X, target.Location.Y) * 5);

                            var projectile = new Projectile(1445);
                            projectile.SetSenderData(Owner, 30);
                            projectile.SetReceiverData(target, 20);
                            projectile.SetFlyingProperties(50, delay, 15, 192, false);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetRangeDamage(target);
                            dmg = target.Combat.IncomingAttack(Owner, DamageType.FullSummoning, dmg, (byte)(delay + 41));
                            Owner.QueueTask(new RsTask(() =>
                                {
                                    var soak = -1;
                                    var damage = target.Combat.Attack(Owner, DamageType.FullSummoning, dmg, ref soak);
                                    var splat = new HitSplat(Summoner);
                                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage,
                                        damage == -1 ? 0 : damage,
                                        ((INpcCombat)Owner.Combat).GetRangeMaxHit(target) <= damage);
                                    if (soak != -1)
                                    {
                                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                    }

                                    target.QueueHitSplat(splat);
                                },
                                CreatureHelper.CalculateTicksForClientTicks(delay + 41)));
                            break;
                        }
                    case 2:
                        {
                            // Magic attack

                            Owner.QueueAnimation(Animation.Create(8190));
                            Owner.QueueGraphic(Graphic.Create(1451));

                            var delay = (byte)Math.Max(10,
                                (int)Location.GetDistance(Owner.Location.X, Owner.Location.Y, target.Location.X, target.Location.Y) * 5);

                            var projectile = new Projectile(1453);
                            projectile.SetSenderData(Owner, 30);
                            projectile.SetReceiverData(target, 20);
                            projectile.SetFlyingProperties(50, delay, 15, 192);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 268);
                            dmg = target.Combat.IncomingAttack(Owner, DamageType.FullSummoning, dmg, delay);
                            Owner.QueueTask(new RsTask(() =>
                                {
                                    var soak = -1;
                                    var damage = target.Combat.Attack(Owner, DamageType.FullSummoning, dmg, ref soak);
                                    var splat = new HitSplat(Summoner);
                                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage,
                                        damage == -1 ? 0 : damage,
                                        ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 268) <= damage);
                                    if (soak != -1)
                                    {
                                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                    }

                                    target.QueueHitSplat(splat);
                                },
                                CreatureHelper.CalculateTicksForClientTicks(delay)));
                            break;
                        }
                }

                GenerateAttackType(target);
            }
        }

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        /// <param name="target">The target.</param>
        private void GenerateAttackType(ICreature target)
        {
            var random = RandomStatic.Generator.NextDouble();
            var meleeChance = 0.5;
            if (Owner.WithinRange(target, 1))
            {
                meleeChance += 0.1;
            }

            const double rangedChance = 0.5;
            const double magicChance = 0.25;
            if (RandomStatic.Generator.NextDouble() <= meleeChance)
            {
                _attackType = 0;
            }
            else if (RandomStatic.Generator.NextDouble() <= rangedChance)
            {
                _attackType = 1;
            }
            else if (RandomStatic.Generator.NextDouble() <= magicChance)
            {
                _attackType = 2;
            }
        }

        /// <summary>
        ///     Get's attack speed of this npc.
        ///     By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackSpeed() => base.GetAttackSpeed();

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
                case 0: return 1;
                case 1:
                case 2:
                    return 7;
                default: return base.GetAttackDistance();
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
                case 0: return AttackStyle.MeleeAccurate;
                case 1: return AttackStyle.RangedAccurate;
                case 2: return AttackStyle.MagicNormal;
                default: return AttackStyle.RangedAccurate;
            }
        }

        /// <summary>
        ///     Get's called when npc is teleported.
        /// </summary>
        public override void OnTeleport()
        {
            Owner.QueueAnimation(Animation.Create(8188));
            base.OnTeleport();
        }

        /// <summary>
        ///     Sets the special move target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void SetSpecialMoveTarget(IRuneObject? target)
        {
            if (!SpecialMoveClicked())
            {
                return;
            }

            Owner.QueueAnimation(Animation.Create(8196));
            Owner.QueueGraphic(Graphic.Create(1449));
        }

        /// <summary>
        ///     Get's if this npc can be poisoned.
        ///     By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can poison; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanPoison() => false;

        /// <summary>
        ///     Get's amount of special energy required by this FamiliarScript.
        ///     By default , this method does throw NotImplementedException
        /// </summary>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialMovePoints() => 12;

        /// <summary>
        ///     Gets the type of the special.
        /// </summary>
        /// <returns>
        ///     SpecialType
        /// </returns>
        public override FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Click;

        /// <summary>
        ///     Gets the name of the special attack.
        /// </summary>
        /// <returns>
        ///     The special attack name
        /// </returns>
        public override string GetSpecialMoveName() => "Steel of Legends";

        /// <summary>
        ///     Gets the special attack description.
        /// </summary>
        /// <returns>
        ///     The special attack description
        /// </returns>
        public override string GetSpecialMoveDescription() =>
            "Defence boost only applies to melee attacks. Scroll initiates attack on opponent, hitting four times, with either ranged or melee, depending on the distance to the target.";
    }
}