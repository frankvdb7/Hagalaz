using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Fiends
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([5361])]
    public class Waterfiend : NpcScriptBase
    {
        private readonly IProjectileBuilder _projectileBuilder;

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

        public Waterfiend(IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
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
                case AttackType.Magic: return AttackBonus.Magic;
                case AttackType.Ranged: return AttackBonus.Ranged;
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
                case AttackType.Magic: return AttackStyle.MagicNormal;
                case AttackType.Ranged: return AttackStyle.RangedAccurate;
                default: return base.GetAttackStyle();
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
                        const int magicMax = 250;
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

                        var delay = 30 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(2705)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(delay)
                            .WithFromHeight(35)
                            .WithToHeight(35)
                            .WithDelay(10)
                            .WithSlope(5)
                            .Send();

                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, magicMax),
                            MaxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, magicMax),
                            Delay = delay,
                            DamageType = DamageType.StandardMagic,
                            Target = target
                        });
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

                        var delay = 30 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(2705)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(delay)
                            .WithFromHeight(35)
                            .WithToHeight(35)
                            .WithDelay(10)
                            .WithSlope(5)
                            .Send();

                        var handle = Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetRangeDamage(target),
                            MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                            Delay = delay,
                            DamageType = DamageType.StandardRange,
                            Target = target
                        });

                        handle.RegisterResultHandler(_ =>
                        {
                            target.QueueGraphic(Graphic.Create(2710, 0, 100));
                        });
                        break;
                    }
            }

            _type = RandomStatic.Generator.Next(0, 2) == 1 ? AttackType.Ranged : AttackType.Magic;
        }
    }
}