using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
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
        private readonly IPathFinder _pathFinder;

        private readonly IProjectileBuilder _projectileBuilder;
        private readonly IMapRegionService _mapRegionService;

        public CorporealBeast(IPathFinder pathFinder, IProjectileBuilder projectileBuilder, IMapRegionService mapRegionService)
        {
            _pathFinder = pathFinder;
            _projectileBuilder = projectileBuilder;
            _mapRegionService = mapRegionService;
        }

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

                        var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
                        var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
                        Owner.Combat.PerformAttack(new AttackParams
                        {
                            Target = target, Damage = damage, DamageType = DamageType.StandardMelee, MaxDamage = maxDamage
                        });
                        break;
                    }
                case Attacks.Claw2:
                    {
                        Owner.QueueAnimation(Animation.Create(10058));
                        var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
                        var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
                        Owner.Combat.PerformAttack(new AttackParams
                        {
                            Target = target, Damage = damage, DamageType = DamageType.StandardMelee, MaxDamage = maxDamage
                        });
                        break;
                    }
                case Attacks.Trample:
                    {
                        Owner.QueueAnimation(Animation.Create(10066));
                        Owner.QueueGraphic(Graphic.Create(1834));

                        var visibleCharacters = Owner.Viewport.VisibleCreatures
                            .OfType<ICharacter>()
                            .Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner) && Owner.WithinRange(c, GetAttackDistance()));
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

                            var delay = 15 + deltaX * 2 + deltaY * 2;

                            var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(c);
                            var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(c);
                            Owner.Combat.PerformAttack(new AttackParams
                            {
                                Target = c,
                                Damage = damage,
                                DamageType = DamageType.StandardMelee,
                                MaxDamage = maxDamage,
                                Delay = delay
                            });
                        }

                        break;
                    }
                case Attacks.Killer:
                    {
                        Owner.QueueAnimation(Animation.Create(10059));

                        var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>()
                            .Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
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

                            _projectileBuilder
                                .Create()
                                .WithGraphicId(1825)
                                .FromCreature(Owner)
                                .ToCreature(c)
                                .WithDuration(40)
                                .WithFromHeight(50)
                                .WithToHeight(35)
                                .WithDelay(delay)
                                .WithSlope(0)
                                .WithAngle(192)
                                .Send();

                            var damage = ((INpcCombat)Owner.Combat).GetMagicDamage(c, 650);
                            var maxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(c, 650);
                            Owner.Combat.PerformAttack(new AttackParams
                            {
                                Target = c,
                                Damage = damage,
                                DamageType = DamageType.StandardMagic,
                                MaxDamage = maxDamage,
                                Delay = delay
                            });
                        }

                        break;
                    }
                case Attacks.StatReduce:
                    {
                        // TODO - Reduce stats.

                        Owner.QueueAnimation(Animation.Create(10059));

                        var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>()
                            .Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
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

                            _projectileBuilder
                                .Create()
                                .WithGraphicId(1823)
                                .FromCreature(Owner)
                                .ToCreature(c)
                                .WithDuration(40)
                                .WithFromHeight(50)
                                .WithToHeight(35)
                                .WithDelay(delay)
                                .WithSlope(0)
                                .WithAngle(192)
                                .Send();

                            var damage = ((INpcCombat)Owner.Combat).GetMagicDamage(c, 600);
                            var maxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(c, 600);
                            Owner.Combat.PerformAttack(new AttackParams
                            {
                                Target = c,
                                Damage = damage,
                                DamageType = DamageType.StandardMagic,
                                MaxDamage = maxDamage,
                                Delay = delay
                            });
                        }

                        break;
                    }
                case Attacks.Splash:
                    {
                        Owner.QueueAnimation(Animation.Create(10059));

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


                        _projectileBuilder
                            .Create()
                            .WithGraphicId(1824)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(40)
                            .WithFromHeight(50)
                            .WithToHeight(35)
                            .WithDelay(delay)
                            .WithSlope(0)
                            .WithAngle(192)
                            .Send();

                        var damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 550);
                        var maxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 550);
                        // the normal hit
                        Owner.Combat.PerformAttack(new AttackParams
                        {
                            Target = target, Damage = damage, DamageType = DamageType.StandardMagic, MaxDamage = maxDamage
                        });

                        Owner.QueueTask(new RsTask(() =>
                            {
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
                                    if (!_mapRegionService.IsAccessible(current))
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

                                    var delay2 = 40 + dX * 2 + dY * 2;
                                    _projectileBuilder
                                        .Create()
                                        .WithGraphicId(1824)
                                        .FromCreature(target)
                                        .ToLocation(location)
                                        .WithDuration(1)
                                        .WithFromHeight(35)
                                        .WithToHeight(0)
                                        .WithSlope(0)
                                        .WithAngle(192)
                                        .Send();

                                    var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>()
                                        .Where(c => IsAggressiveTowards(c) && c.Location.Equals(location));
                                    foreach (var character in visibleCharacters)
                                    {
                                        var damage2 = ((INpcCombat)Owner.Combat).GetMagicDamage(character, 550);
                                        var maxDamage2 = ((INpcCombat)Owner.Combat).GetMagicMaxHit(character, 550);
                                        Owner.Combat.PerformAttack(new AttackParams
                                        {
                                            Target = character,
                                            Damage = damage2,
                                            DamageType = DamageType.StandardMagic,
                                            MaxDamage = maxDamage2,
                                            Delay = delay2
                                        });
                                    }
                                }
                            },
                            CreatureHelper.CalculateTicksForClientTicks(delay)));
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
        private void GenerateAttackType(ICreature? target)
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
                if (!(hitValue < runningValue))
                {
                    continue;
                }

                switch (i)
                {
                    case 0:
                        _attackType = Attacks.Killer;
                        return;
                    case 1:
                        _attackType = Attacks.StatReduce;
                        return;
                    case 2:
                        _attackType = Attacks.Splash;
                        return;
                }

                break;
            }

            _attackType = RandomStatic.Generator.NextDouble() <= 0.50 ? Attacks.Claw2 : Attacks.Claw1;
        }

        /// <summary>
        ///     Called when [cancel target].
        /// </summary>
        public override void OnCancelTarget()
        {
            AggressivenessTick();
            // no possible targets found, resetting.
            if (Owner.Combat.Target != null)
            {
                return;
            }

            base.OnCancelTarget();
            Owner.Statistics.Normalise();
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
        protected override void Initialize() => GenerateAttackType(null);
    }
}