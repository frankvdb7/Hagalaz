using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Hagalaz.Game.Scripts.Npcs.Bosses
{
    /// <summary>
    /// </summary>
    internal enum Attacks
    {
        /// <summary>
        ///     The trample.
        /// </summary>
        Trample,

        /// <summary>
        ///     The claw1 (or the 'normal' attack)
        /// </summary>
        Claw1,

        /// <summary>
        ///     The claw2.
        /// </summary>
        Claw2,

        /// <summary>
        ///     The killer.
        /// </summary>
        Killer,

        /// <summary>
        ///     The stat reduce.
        /// </summary>
        StatReduce,

        /// <summary>
        ///     The splash.
        /// </summary>
        Splash
    }

    [NpcScriptMetaData([8133])]
    public class CorporealBeast : NpcScriptBase
    {
        /// <summary>
        ///     The attack type.
        /// </summary>
        private Attacks _attackType = Attacks.Claw1;

        /// <summary>
        ///     The path finder
        /// </summary>
        private IPathFinder _pathFinder;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            switch (_attackType)
            {
                case Attacks.Claw1:
                    {
                        Owner.QueueAnimation(Animation.Create(10057));
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
                case Attacks.Claw2:
                    {
                        Owner.QueueAnimation(Animation.Create(10058));
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
                case Attacks.Trample:
                    {
                        Owner.QueueAnimation(Animation.Create(10066));
                        Owner.QueueGraphic(Graphic.Create(1834));

                        var combat = (INpcCombat)Owner.Combat;

                        var visibleCharacters = Owner.Viewport.VisibleCreatures
                            .OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner) && Owner.WithinRange(c, GetAttackDistance()));
                        foreach (var c in visibleCharacters)
                        {
                            var deltaX = Owner.Location.X - c.Location.X;
                            var deltaY = Owner.Location.Y - c.Location.Y;
                            if (deltaX < 0)
                            {
                                deltaX = -deltaX;
                            }

                            if (deltaY < 0)
                            {
                                deltaY = -deltaY;
                            }

                            var delay = (byte)(15 + deltaX * 2 + deltaY * 2);

                            var dmg = ((INpcCombat)Owner.Combat).GetMeleeDamage(c);
                            dmg = c.Combat.IncomingAttack(Owner, DamageType.StandardMelee, dmg, delay);

                            var toAttack = c;
                            Owner.QueueTask(new RsTask(() =>
                                {
                                    var soak = -1;
                                    var damage = toAttack.Combat.Attack(Owner, DamageType.StandardMelee, dmg, ref soak);
                                    var splat = new HitSplat(Owner);
                                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMeleeMaxHit(toAttack) <= damage);
                                    if (soak != -1)
                                    {
                                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                    }

                                    toAttack.QueueHitSplat(splat);
                                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                        }

                        break;
                    }
                case Attacks.Killer:
                    {
                        Owner.QueueAnimation(Animation.Create(10059));

                        var combat = (INpcCombat)Owner.Combat;

                        var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
                        foreach (var c in visibleCharacters)
                        {
                            var deltaX = Owner.Location.X - c.Location.X;
                            var deltaY = Owner.Location.Y - c.Location.Y;
                            if (deltaX < 0)
                            {
                                deltaX = -deltaX;
                            }

                            if (deltaY < 0)
                            {
                                deltaY = -deltaY;
                            }

                            var delay = 75 + deltaX * 2 + deltaY * 2;

                            var projectile = new Projectile(1825);
                            projectile.SetSenderData(Owner, 50, false);
                            projectile.SetReceiverData(c, 35);
                            projectile.SetFlyingProperties(delay, 40, 0, 192, false);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(c, 650);
                            dmg = c.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

                            var toAttack = c;
                            Owner.QueueTask(new RsTask(() =>
                                {
                                    var soak = -1;
                                    var damage = toAttack.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                                    var splat = new HitSplat(Owner);
                                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicMaxHit(toAttack, 650) <= damage);
                                    if (soak != -1)
                                    {
                                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                    }

                                    toAttack.QueueHitSplat(splat);
                                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                        }

                        break;
                    }
                case Attacks.StatReduce:
                    {
                        // TODO - Reduce stats.

                        Owner.QueueAnimation(Animation.Create(10059));

                        var combat = (INpcCombat)Owner.Combat;

                        var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
                        foreach (var c in visibleCharacters)
                        {
                            var deltaX = Owner.Location.X - c.Location.X;
                            var deltaY = Owner.Location.Y - c.Location.Y;
                            if (deltaX < 0)
                            {
                                deltaX = -deltaX;
                            }

                            if (deltaY < 0)
                            {
                                deltaY = -deltaY;
                            }

                            var delay = 75 + deltaX * 2 + deltaY * 2;

                            var projectile = new Projectile(1823);
                            projectile.SetSenderData(Owner, 50, false);
                            projectile.SetReceiverData(c, 35);
                            projectile.SetFlyingProperties(delay, 40, 0, 192, false);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(c, 600);
                            dmg = c.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

                            var toAttack = c;
                            Owner.QueueTask(new RsTask(() =>
                                {
                                    var soak = -1;
                                    var damage = toAttack.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                                    var splat = new HitSplat(Owner);
                                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicMaxHit(toAttack, 600) <= damage);
                                    if (soak != -1)
                                    {
                                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                    }

                                    toAttack.QueueHitSplat(splat);
                                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                        }

                        break;
                    }
                case Attacks.Splash:
                    {
                        Owner.QueueAnimation(Animation.Create(10059));

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

                        var delay = 75 + deltaX * 2 + deltaY * 2;


                        var projectile = new Projectile(1824);
                        projectile.SetSenderData(Owner, 50, false);
                        projectile.SetReceiverData(target, 35);
                        projectile.SetFlyingProperties(delay, 40, 0, 192, false);
                        projectile.Display();

                        var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 550);
                        dmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

                        var regionService = Owner.ServiceProvider.GetRequiredService<IMapRegionService>();

                        Owner.QueueTask(new RsTask(() =>
                            {
                                // the normal hit
                                var soak = -1;
                                var damage = target.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                                var splat = new HitSplat(Owner);
                                splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 550) <= damage);
                                if (soak != -1)
                                {
                                    splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                }

                                target.QueueHitSplat(splat);

                                // the splashes
                                var locations = new List<ILocation>();
                                var locAmount = RandomStatic.Generator.Next(2, 5);
                                while (locations.Count != locAmount)
                                {
                                    var curX = target.Location.X;
                                    var curY = target.Location.Y;
                                    var xType = RandomStatic.Generator.Next(0, 2);
                                    var yType = RandomStatic.Generator.Next(0, 2);
                                    var xOffset = RandomStatic.Generator.Next(1, 4);
                                    var yOffset = RandomStatic.Generator.Next(1, 4);
                                    if (xType == 1)
                                    {
                                        curX += -xOffset;
                                    }
                                    else
                                    {
                                        curX += xOffset;
                                    }

                                    if (yType == 1)
                                    {
                                        curY += -yOffset;
                                    }
                                    else
                                    {
                                        curY += yOffset;
                                    }

                                    var current = Location.Create(curX, curY, target.Location.Z, target.Location.Dimension);
                                    if (!regionService.IsAccessible(current))
                                    {
                                        continue;
                                    }

                                    var add = !current.Equals(target.Location);
                                    if (!add)
                                    {
                                        continue;
                                    }

                                    if (locations.Contains(current))
                                    {
                                        add = false;
                                    }

                                    if (add)
                                    {
                                        locations.Add(current);
                                    }
                                }

                                foreach (var location in locations)
                                {
                                    var dX = target.Location.X - location.X;
                                    var dY = target.Location.Y - location.Y;
                                    if (dX < 0)
                                    {
                                        dX = -dX;
                                    }

                                    if (dY < 0)
                                    {
                                        dY = -dY;
                                    }

                                    var d = (byte)(40 + dX * 2 + dY * 2);
                                    var proj = new Projectile(1824);
                                    proj.SetSenderData(target, 35, false);
                                    proj.SetReceiverData(location, 0);
                                    proj.SetFlyingProperties(1, 50, 0, 192, false);
                                    proj.Display();

                                    var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && c.Location.Equals(location));
                                    foreach (var character in visibleCharacters)
                                    {
                                        var dmg2 = ((INpcCombat)Owner.Combat).GetMagicDamage(character, 550);
                                        dmg2 = character.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg2, delay);

                                        Owner.QueueTask(new RsTask(() =>
                                            {
                                                // the normal hit
                                                var soa = -1;
                                                var damage2 = character.Combat.Attack(Owner, DamageType.StandardMagic, dmg2, ref soa);
                                                var s = new HitSplat(Owner);
                                                s.SetFirstSplat(damage2 == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage2 == -1 ? 0 : damage2, ((INpcCombat)Owner.Combat).GetMagicMaxHit(character, 550) <= damage2);
                                                if (soa != -1)
                                                {
                                                    s.SetSecondSplat(HitSplatType.HitDefendedDamage, soa, false);
                                                }

                                                character.QueueHitSplat(s);
                                            }, CreatureHelper.CalculateTicksForClientTicks(d)));
                                    }
                                }
                            }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                        break;
                    }
            }

            GenerateAttackType(target);
        }

        /// <summary>
        ///     Contains the route finder the NPC will use.
        /// </summary>
        /// <returns></returns>
        public override IPathFinder GetPathfinder() => _pathFinder;

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        /// <param name="target">The target.</param>
        private void GenerateAttackType(ICreature target)
        {
            // check wether the beast should use trample if the character is too close.
            if (target != null && Owner.WithinRange(target, 0))
            {
                _attackType = Attacks.Trample;
                return;
            }

            var chances = new double[5];
            chances[0] = 0.075; // killer
            chances[1] = 0.075; // stats reduce
            chances[2] = 0.075; // splash
            chances[3] = 0.25; // nothing
            chances[4] = 0.25; // nothing
            if (target != null && !Owner.WithinRange(target, 1))
            {
                chances[0] += 0.05;
                chances[1] += 0.05;
                chances[2] += 0.05;
            }

            var hitValue = Random.Shared.Next(chances.Sum());
            var runningValue = 0.0;
            for (var i = 0; i < chances.Length; i++)
            {
                runningValue += chances[i];
                if (hitValue < runningValue)
                {
                    if (i == 0)
                    {
                        _attackType = Attacks.Killer;
                        return;
                    }

                    if (i == 1)
                    {
                        _attackType = Attacks.StatReduce;
                        return;
                    }

                    if (i == 2)
                    {
                        _attackType = Attacks.Splash;
                        return;
                    }

                    break;
                }
            }

            if (RandomStatic.Generator.NextDouble() <= 0.50)
            {
                _attackType = Attacks.Claw2;
            }
            else
            {
                _attackType = Attacks.Claw1;
            }
        }

        /// <summary>
        ///     Called when [cancel target].
        /// </summary>
        public override void OnCancelTarget()
        {
            AggressivenessTick();
            // no possible targets found, resetting.
            if (Owner.Combat.Target == null)
            {
                base.OnCancelTarget();
                Owner.Statistics.Normalise();
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
            if (_attackType == Attacks.Claw1 || _attackType == Attacks.Claw2)
            {
                return 1;
            }

            if (_attackType == Attacks.Trample)
            {
                return 1;
            }

            if (_attackType == Attacks.Killer || _attackType == Attacks.StatReduce || _attackType == Attacks.Splash)
            {
                return 8;
            }

            return base.GetAttackDistance();
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
            if (_attackType == Attacks.Claw1 || _attackType == Attacks.Claw2)
            {
                return AttackBonus.Slash;
            }

            if (_attackType == Attacks.Trample)
            {
                return AttackBonus.Crush;
            }

            if (_attackType == Attacks.Killer || _attackType == Attacks.StatReduce || _attackType == Attacks.Splash)
            {
                return AttackBonus.Magic;
            }

            return base.GetAttackBonusType();
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
            if (_attackType == Attacks.Claw1 || _attackType == Attacks.Claw2)
            {
                return AttackStyle.MeleeAccurate;
            }

            if (_attackType == Attacks.Trample)
            {
                return AttackStyle.MeleeAggressive;
            }

            if (_attackType == Attacks.Killer || _attackType == Attacks.StatReduce || _attackType == Attacks.Splash)
            {
                return AttackStyle.MagicNormal;
            }

            return base.GetAttackStyle();
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
        ///     Get's if this npc can be poisoned.
        ///     By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can poison; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanPoison() => false;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
            _pathFinder = Owner.ServiceProvider.GetRequiredService<ISmartPathFinder>();
            GenerateAttackType(null);
        }
    }
}