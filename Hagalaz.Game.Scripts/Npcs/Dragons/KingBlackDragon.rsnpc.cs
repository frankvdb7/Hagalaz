using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

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
    [NpcScriptMetaData([50])]
    public class KingBlackDragon : NpcScriptBase
    {
        private readonly IHitSplatBuilder _hitSplatBuilder;
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     The attack type.
        /// </summary>
        private Attacks _attackType = Attacks.Normal;

        public KingBlackDragon(IHitSplatBuilder hitSplatBuilder, IProjectileBuilder projectileBuilder)
        {
            _hitSplatBuilder = hitSplatBuilder;
            _projectileBuilder = projectileBuilder;
        }

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
                        var mDamage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = mDamage,
                            MaxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target),
                            DamageType = DamageType.StandardMelee,
                            Target = target,
                        });
                        break;
                    }
                case Attacks.NormalDragonFire:
                    {
                        const int maxHit = 650;
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

                        var duration = 50 + deltaX * 2 + deltaY * 2;

                        _projectileBuilder
                            .Create()
                            .WithGraphicId(3442)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(duration)
                            .WithDelay(50)
                            .WithToHeight(41)
                            .WithFromHeight(60)
                            .Send();

                        var result = Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit),
                            MaxDamage = maxHit,
                            DamageType = DamageType.DragonFire,
                            Target = target,
                            Delay = duration
                        });
                        result.RegisterResultHandler(attackResult =>
                        {
                            if (attackResult.DamageLifePoints.Succeeded)
                            {
                                target.QueueGraphic(Graphic.Create(3443));
                            }
                        });
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

                        var duration = 50 + deltaX * 2 + deltaY * 2;

                        _projectileBuilder
                            .Create()
                            .WithGraphicId(3436)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(duration)
                            .WithToHeight(41)
                            .WithFromHeight(60)
                            .WithDelay(25)
                            .Send();


                        var result = Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit),
                            MaxDamage = maxHit,
                            DamageType = DamageType.DragonFire,
                            Target = target,
                            Delay = duration
                        });

                        result.RegisterResultHandler(attackResult =>
                        {
                            if (attackResult.DamageLifePoints.Succeeded)
                            {
                                target.QueueGraphic(Graphic.Create(3437));
                                target.Poison(80);
                            }
                        });
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

                        var duration = 50 + deltaX * 2 + deltaY * 2;

                        _projectileBuilder
                            .Create()
                            .WithGraphicId(3433)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(duration)
                            .WithDelay(30)
                            .WithToHeight(41)
                            .WithFromHeight(60)
                            .Send();

                        var result = Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit),
                            MaxDamage = maxHit,
                            DamageType = DamageType.DragonFire,
                            Target = target,
                            Delay = duration
                        });

                        result.RegisterResultHandler(attackResult =>
                        {
                            if (attackResult.DamageLifePoints.Succeeded && target is ICharacter character)
                            {
                                target.QueueGraphic(Graphic.Create(3434));
                                for (var i = 0; i < StatisticsConstants.SkillsCount; i++)
                                {
                                    character.Statistics.DamageSkill(i, 2);
                                }
                            }
                        });
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

                        var duration = 50 + deltaX * 2 + deltaY * 2;

                        _projectileBuilder
                            .Create()
                            .WithGraphicId(3439)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(duration)
                            .WithDelay(25)
                            .WithToHeight(41)
                            .WithFromHeight(60)
                            .Send();

                        var result = Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit),
                            MaxDamage = maxHit,
                            DamageType = DamageType.DragonFire,
                            Target = target,
                            Delay = duration
                        });

                        result.RegisterResultHandler(attackResult =>
                        {
                            target.QueueGraphic(Graphic.Create(3440));

                            if (attackResult.DamageLifePoints.Succeeded)
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
                        });
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
    }
}