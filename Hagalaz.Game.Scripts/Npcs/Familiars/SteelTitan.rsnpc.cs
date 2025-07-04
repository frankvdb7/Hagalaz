using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    [NpcScriptMetaData([7343, 7344])]
    public class SteelTitan : FamiliarScriptBase
    {
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     The attack type
        ///     0 = Melee.
        ///     1 = Ranged.
        ///     2 = Magic.
        /// </summary>
        private int _attackType;

        public SteelTitan(ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService, IProjectileBuilder projectileBuilder) : base(
            pathFinder,
            npcService,
            itemService) =>
            _projectileBuilder = projectileBuilder;

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

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim),
                MaxDamage = useMelee ? combat.GetMeleeMaxHit(victim) : combat.GetRangeMaxHit(victim),
                DamageType = DamageType.FullSummoning,
                Target = victim,
                Delay = 0
            });

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim),
                MaxDamage = useMelee ? combat.GetMeleeMaxHit(victim) : combat.GetRangeMaxHit(victim),
                DamageType = DamageType.FullSummoning,
                Target = victim,
                Delay = 0
            });

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim),
                MaxDamage = useMelee ? combat.GetMeleeMaxHit(victim) : combat.GetRangeMaxHit(victim),
                DamageType = DamageType.FullSummoning,
                Target = victim,
                Delay = 1
            });

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = useMelee ? combat.GetMeleeDamage(victim) : combat.GetRangeDamage(victim),
                MaxDamage = useMelee ? combat.GetMeleeMaxHit(victim) : combat.GetRangeMaxHit(victim),
                DamageType = DamageType.FullSummoning,
                Target = victim,
                Delay = 1
            });
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
                            Owner.Combat.PerformAttack(new AttackParams()
                            {
                                Damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target),
                                MaxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target),
                                DamageType = DamageType.FullSummoning,
                                Target = target,
                                Delay = 0
                            });
                            break;
                        }
                    case 1:
                        {
                            // Ranged attack

                            Owner.QueueAnimation(Animation.Create(8190));

                            var delay = Math.Max(10, (int)Location.GetDistance(Owner.Location.X, Owner.Location.Y, target.Location.X, target.Location.Y) * 5);

                            _projectileBuilder.Create()
                                .WithGraphicId(1445)
                                .FromCreature(Owner)
                                .ToCreature(target)
                                .WithDuration(delay)
                                .WithFromHeight(30)
                                .WithToHeight(20)
                                .WithDelay(50)
                                .WithSlope(15)
                                .WithAngle(192)
                                .Send();

                            Owner.Combat.PerformAttack(new AttackParams()
                            {
                                Damage = ((INpcCombat)Owner.Combat).GetRangeDamage(target),
                                MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                                DamageType = DamageType.FullSummoning,
                                Delay = delay + 41,
                                Target = target
                            });
                            break;
                        }
                    case 2:
                        {
                            // Magic attack

                            Owner.QueueAnimation(Animation.Create(8190));
                            Owner.QueueGraphic(Graphic.Create(1451));

                            var delay = Math.Max(10, (int)Location.GetDistance(Owner.Location.X, Owner.Location.Y, target.Location.X, target.Location.Y) * 5);

                            _projectileBuilder.Create()
                                .WithGraphicId(1453)
                                .FromCreature(Owner)
                                .ToCreature(target)
                                .WithDuration(delay)
                                .WithFromHeight(30)
                                .WithToHeight(20)
                                .WithDelay(50)
                                .WithSlope(15)
                                .WithAngle(192)
                                .Send();

                            Owner.Combat.PerformAttack(new AttackParams()
                            {
                                Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 268),
                                MaxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 268),
                                DamageType = DamageType.FullSummoning,
                                Target = target,
                                Delay = delay + 41
                            });
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