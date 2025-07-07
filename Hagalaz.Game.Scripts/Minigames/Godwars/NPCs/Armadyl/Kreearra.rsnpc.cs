using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl
{
    [NpcScriptMetaData([6222])]
    public class Kreearra : General
    {
        private readonly ILocationBuilder _locationBuilder;
        private readonly IAnimationBuilder _animationBuilder;
        private readonly IMovementBuilder _movementBuilder;
        private readonly IProjectileBuilder _projectileBuilder;
        private readonly IMapRegionService _mapRegionService;

        /// <summary>
        /// </summary>
        private enum Attack
        {
            Melee,
            Magic,
            Ranged
        }

        /// <summary>
        ///     The attack.
        /// </summary>
        private Attack _attack;

        public Kreearra(
            ILocationBuilder locationBuilder, IAnimationBuilder animationBuilder, IMovementBuilder movementBuilder, IProjectileBuilder projectileBuilder,
            IMapRegionService mapRegionService)
        {
            _locationBuilder = locationBuilder;
            _animationBuilder = animationBuilder;
            _movementBuilder = movementBuilder;
            _projectileBuilder = projectileBuilder;
            _mapRegionService = mapRegionService;
        }

        /// <summary>
        ///     Get's if this npc can be attacked by the specified character ('attacker').
        ///     By default , this method does check if this npc is attackable.
        ///     This method also checks if the attacker is a character, wether or not it
        ///     has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            var style = attacker.Combat.GetAttackStyle();
            if (style != AttackStyle.MeleeAccurate && style != AttackStyle.MeleeAggressive && style != AttackStyle.MeleeControlled &&
                style != AttackStyle.MeleeDefensive)
            {
                return base.CanBeAttackedBy(attacker);
            }

            if (attacker is ICharacter character)
            {
                character.SendChatMessage("The aviansie is flying too high for you to attack using melee.");
            }

            if (CanRetaliateTo(attacker))
            {
                Owner.QueueTask(new RsTask(() => Owner.Combat.SetTarget(attacker), 1));
            }

            return false;
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() =>
            _attack switch
            {
                Attack.Melee => AttackBonus.Slash,
                Attack.Magic => AttackBonus.Magic,
                Attack.Ranged => AttackBonus.Ranged,
                _ => base.GetAttackBonusType()
            };

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => _attack == Attack.Melee ? 1 : 7;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            var combat = (INpcCombat)Owner.Combat;
            switch (_attack)
            {
                case Attack.Melee:
                    {
                        RenderAttack();

                        var maxDamage = combat.GetMeleeMaxHit(target);
                        Owner.Combat.PerformAttack(new AttackParams
                        {
                            Target = target, DamageType = DamageType.StandardMelee, Damage = combat.GetMeleeDamage(target, maxDamage), MaxDamage = maxDamage
                        });

                        Owner.Combat.PerformAttack(new AttackParams
                        {
                            Target = target, DamageType = DamageType.StandardMelee, Damage = combat.GetMeleeDamage(target, maxDamage), MaxDamage = maxDamage
                        });
                        break;
                    }
                case Attack.Magic:
                    {
                        Owner.QueueAnimation(_animationBuilder.Create().WithId(6976).Build());

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

                            var delay = 25 + deltaX * 2 + deltaY * 2;

                            _projectileBuilder
                                .Create()
                                .WithGraphicId(1198)
                                .FromCreature(Owner)
                                .ToCreature(c)
                                .WithDuration(delay)
                                .WithAngle(180)
                                .Send();

                            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(c, 210);

                            Owner.Combat.PerformAttack(new AttackParams
                            {
                                Target = c,
                                DamageType = DamageType.StandardMagic,
                                Damage = dmg,
                                MaxDamage = combat.GetMagicMaxHit(c, 210),
                                Delay = delay
                            });
                        }

                        break;
                    }

                case Attack.Ranged:
                    {
                        Owner.QueueAnimation(_animationBuilder.Create().WithId(6976).Build());

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

                            var delay = 25 + deltaX * 2 + deltaY * 2;

                            _projectileBuilder
                                .Create()
                                .WithGraphicId(1197)
                                .FromCreature(Owner)
                                .ToCreature(c)
                                .WithDuration(delay)
                                .WithDelay(40)
                                .WithAngle(180)
                                .Send();

                            var dmg = ((INpcCombat)Owner.Combat).GetRangeDamage(c);

                            Owner.Combat.PerformAttack(new AttackParams()
                            {
                                Damage = dmg,
                                Delay = delay,
                                MaxDamage = combat.GetRangeMaxHit(c),
                                Target = c,
                                DamageType = DamageType.StandardRange,
                            });

                            Owner.QueueTask(new RsTask(() =>
                                {
                                    var direction = Owner.Location.GetDirection(c.Location);
                                    var diffX = 0;
                                    var diffY = 0;
                                    var moveDirection = FaceDirection.None;
                                    switch (direction)
                                    {
                                        case DirectionFlag.North:
                                            // north
                                            moveDirection = FaceDirection.South;
                                            diffY = 1;
                                            break;
                                        case DirectionFlag.East:
                                            // east
                                            moveDirection = FaceDirection.West;
                                            diffX = 1;
                                            break;
                                        case DirectionFlag.South:
                                            // south
                                            moveDirection = FaceDirection.North;
                                            diffY = -1;
                                            break;
                                        case DirectionFlag.West:
                                            // west
                                            moveDirection = FaceDirection.East;
                                            diffX = -1;
                                            break;
                                        // north-east
                                        case DirectionFlag.NorthEast:
                                            moveDirection = FaceDirection.SouthWest;
                                            diffX = 1;
                                            diffY = 1;
                                            break;
                                        // south-east
                                        case DirectionFlag.SouthEast:
                                            moveDirection = FaceDirection.NorthWest;
                                            diffX = 1;
                                            diffY = -1;
                                            break;
                                        // north-west
                                        case DirectionFlag.NorthWest:
                                            moveDirection = FaceDirection.SouthEast;
                                            diffX = -1;
                                            diffY = 1;
                                            break;
                                        // south-west
                                        case DirectionFlag.SouthWest:
                                            moveDirection = FaceDirection.NorthEast;
                                            diffX = -1;
                                            diffY = -1;
                                            break;
                                    }

                                    var destination = _locationBuilder.Create()
                                        .WithX(c.Location.X + diffX)
                                        .WithY(c.Location.Y + diffY)
                                        .WithZ(c.Location.Z)
                                        .WithDimension(c.Location.Dimension)
                                        .Build();
                                    if (!_mapRegionService.IsAccessible(destination))
                                    {
                                        return;
                                    }

                                    if (destination.X == 2839 || destination.Y == 5295)
                                    {
                                        return;
                                    }

                                    c.Movement.ClearQueue();
                                    c.Movement.Teleport(Teleport.Create(destination, MovementType.Walk));
                                    var mov = _movementBuilder
                                        .Create()
                                        .WithStart(c.Location)
                                        .WithEnd(destination)
                                        .WithStartSpeed(15)
                                        .WithEndSpeed(15)
                                        .WithFaceDirection(moveDirection)
                                        .Build();
                                    c.QueueForceMovement(mov);
                                },
                                CreatureHelper.CalculateTicksForClientTicks(delay)));
                        }

                        break;
                    }
            }

            GenerateAttackType(target);
        }

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        /// <param name="target">The target.</param>
        private void GenerateAttackType(ICreature? target)
        {
            var chances = new double[4];
            chances[0] = 0.50; // ranged
            chances[1] = 0.20; // magic
            chances[2] = 0.0; // melee
            chances[3] = 0.10;
            if (target != null && !Owner.WithinRange(target, 1))
            {
                chances[0] += 0.15;
                chances[1] += 0.15;
            }

            if (!Owner.Combat.RecentAttackers.Any())
            {
                chances[2] += 0.25;
            }

            var hitValue = Random.Shared.Next(chances.Sum());
            var runningValue = 0.0;
            for (var i = 0; i < chances.Length; i++)
            {
                runningValue += chances[i];
                if (hitValue < runningValue)
                {
                    switch (i)
                    {
                        case 0:
                            _attack = Attack.Ranged;
                            return;
                        case 1:
                            _attack = Attack.Magic;
                            return;
                        case 2:
                            _attack = Attack.Melee;
                            return;
                        default: return;
                    }
                }
            }
        }

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() =>
            _attack switch
            {
                Attack.Melee => AttackStyle.MeleeAccurate,
                Attack.Magic => AttackStyle.MagicNormal,
                Attack.Ranged => AttackStyle.RangedLongRange,
                _ => base.GetAttackStyle()
            };
    }
}