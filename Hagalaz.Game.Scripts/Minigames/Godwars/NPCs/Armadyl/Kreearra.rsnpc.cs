using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl
{
    [NpcScriptMetaData([6222])]
    public class Kreearra : General
    {
        private readonly ILocationBuilder _locationBuilder;
        private readonly IAnimationBuilder _animationBuilder;
        private readonly IMovementBuilder _movementBuilder;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationBuilder"></param>
        /// <param name="animationBuilder"></param>
        /// <param name="movementBuilder"></param>
        public Kreearra(ILocationBuilder locationBuilder, IAnimationBuilder animationBuilder, IMovementBuilder movementBuilder)
        {
            _locationBuilder = locationBuilder;
            _animationBuilder = animationBuilder;
            _movementBuilder = movementBuilder;
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
        public override AttackBonus GetAttackBonusType()
        {
            switch (_attack)
            {
                case Attack.Melee: return AttackBonus.Slash;
                case Attack.Magic: return AttackBonus.Magic;
                case Attack.Ranged: return AttackBonus.Ranged;
                default: return base.GetAttackBonusType();
            }
        }

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

                        var hit1 = target.Combat.IncomingAttack(Owner, DamageType.StandardMelee, combat.GetMeleeDamage(target), 0);
                        var hit2 = target.Combat.IncomingAttack(Owner, DamageType.StandardMelee, combat.GetMeleeDamage(target), 0);
                        var standardMax = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
                        var soaked1 = -1;
                        var damage1 = target.Combat.Attack(Owner, DamageType.StandardMelee, hit1, ref soaked1);
                        var soaked2 = -1;
                        var damage2 = target.Combat.Attack(Owner, DamageType.StandardMelee, hit2, ref soaked2);

                        var splat1 = new HitSplat(Owner);
                        splat1.SetFirstSplat(damage1 == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage,
                            damage1 == -1 ? 0 : damage1,
                            standardMax <= damage1);
                        if (soaked1 != -1)
                        {
                            splat1.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked1, false);
                        }

                        target.QueueHitSplat(splat1);

                        var splat2 = new HitSplat(Owner);
                        splat2.SetFirstSplat(damage2 == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage,
                            damage2 == -1 ? 0 : damage2,
                            standardMax <= damage2);
                        if (soaked2 != -1)
                        {
                            splat2.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked2, false);
                        }

                        target.QueueHitSplat(splat2);
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

                            var delay = (byte)(25 + deltaX * 2 + deltaY * 2);

                            var projectile = new Projectile(1198);
                            projectile.SetSenderData(Owner, 0);
                            projectile.SetReceiverData(c, 0);
                            projectile.SetFlyingProperties(40, delay, 0, 180, false);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(c, 210);
                            dmg = c.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

                            var toAttack = c;
                            c.QueueTask(new RsTask(() =>
                                {
                                    var soak = -1;
                                    var damage = toAttack.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                                    var splat = new HitSplat(Owner);
                                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage,
                                        damage == -1 ? 0 : damage,
                                        combat.GetMagicMaxHit(toAttack, 210) <= damage);
                                    if (soak != -1)
                                    {
                                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                    }

                                    toAttack.QueueHitSplat(splat);
                                },
                                CreatureHelper.CalculateTicksForClientTicks(delay)));
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

                            var regionService = Owner.ServiceProvider.GetRequiredService<IMapRegionService>();

                            var delay = 25 + deltaX * 2 + deltaY * 2;

                            var projectile = new Projectile(1197);
                            projectile.SetSenderData(Owner, 0);
                            projectile.SetReceiverData(c, 0);
                            projectile.SetFlyingProperties(40, delay, 0, 180, false);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetRangeDamage(c);
                            dmg = c.Combat.IncomingAttack(Owner, DamageType.StandardRange, dmg, delay);

                            var toAttack = c;
                            Owner.QueueTask(new RsTask(() =>
                                {
                                    var direction = Owner.Location.GetDirection(toAttack.Location);
                                    short diffX = 0;
                                    short diffY = 0;
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
                                        .WithX(toAttack.Location.X + diffX)
                                        .WithY(toAttack.Location.Y + diffY)
                                        .WithZ(toAttack.Location.Z)
                                        .WithDimension(toAttack.Location.Dimension)
                                        .Build();
                                    if (regionService.IsAccessible(destination))
                                    {
                                        if (destination.X != 2839 && destination.Y != 5295)
                                        {
                                            toAttack.Movement.ClearQueue();
                                            toAttack.Movement.Teleport(Teleport.Create(destination, MovementType.Walk));
                                            var mov = _movementBuilder
                                                .Create()
                                                .WithStart(toAttack.Location)
                                                .WithEnd(destination)
                                                .WithStartSpeed(15)
                                                .WithEndSpeed(15)
                                                .WithFaceDirection(moveDirection)
                                                .Build();
                                            toAttack.QueueForceMovement(mov);
                                        }
                                    }

                                    var soak = -1;
                                    var damage = toAttack.Combat.Attack(Owner, DamageType.StandardRange, dmg, ref soak);
                                    var splat = new HitSplat(Owner);
                                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage,
                                        damage == -1 ? 0 : damage,
                                        combat.GetRangeMaxHit(c) <= damage);
                                    if (soak != -1)
                                    {
                                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                    }

                                    toAttack.QueueHitSplat(splat);
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
                    if (i == 0)
                    {
                        _attack = Attack.Ranged;
                        return;
                    }

                    if (i == 1)
                    {
                        _attack = Attack.Magic;
                        return;
                    }

                    if (i == 2)
                    {
                        _attack = Attack.Melee;
                        return;
                    }

                    return;
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
        public override AttackStyle GetAttackStyle()
        {
            switch (_attack)
            {
                case Attack.Melee: return AttackStyle.MeleeAccurate;
                case Attack.Magic: return AttackStyle.MagicNormal;
                case Attack.Ranged: return AttackStyle.RangedLongRange;
                default: return base.GetAttackStyle();
            }
        }
    }
}