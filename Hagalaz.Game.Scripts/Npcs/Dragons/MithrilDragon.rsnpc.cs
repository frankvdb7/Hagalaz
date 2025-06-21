using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Npcs.Dragons
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([5363])]
    public class MithrilDragon : StandardDragon
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public MithrilDragon(IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
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

                    var delay = 30 + deltaX * 5 + deltaY * 5;
                    _projectileBuilder.Create()
                        .WithGraphicId(12)
                        .FromCreature(Owner)
                        .ToCreature(target)
                        .WithDuration(delay)
                        .WithSlope(10)
                        .WithDelay(10)
                        .WithFromHeight(50)
                        .WithToHeight(35)
                        .Send();

                    Owner.Combat.PerformAttack(new AttackParams()
                    {
                        Damage = ((INpcCombat)Owner.Combat).GetRangeDamage(target),
                        MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                        Target = target,
                        DamageType = DamageType.StandardRange,
                        Delay = delay
                    });
                }
                else
                {
                    Bonus = AttackBonus.Magic;
                    Style = AttackStyle.MagicNormal;
                    Owner.QueueAnimation(Animation.Create(14252));

                    const int maxDamage = 550;
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

                    var delay = 20 + deltaX * 5 + deltaY * 5;
                    _projectileBuilder.Create()
                        .WithGraphicId(2705)
                        .FromCreature(Owner)
                        .ToCreature(target)
                        .WithDuration(delay)
                        .WithSlope(10)
                        .WithDelay(10)
                        .WithFromHeight(50)
                        .WithToHeight(35)
                        .Send();

                    var handle = Owner.Combat.PerformAttack(new AttackParams()
                    {
                        Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxDamage),
                        MaxDamage = maxDamage,
                        Target = target,
                        DamageType = DamageType.StandardMagic,
                        Delay = delay
                    });

                    handle.RegisterResultHandler(_ => { target.QueueGraphic(Graphic.Create(2710, 0, 100)); });
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

                    Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit),
                            MaxDamage = maxHit,
                            Target = target,
                            DamageType = DamageType.DragonFire,
                            Delay = 2
                        })
                        .RegisterResultHandler(_ => { target.QueueGraphic(Graphic.Create(439)); });
                }
                else
                {
                    Style = AttackStyle.MeleeAggressive;
                    Bonus = AttackBonus.Slash;
                    RenderAttack();
                    Owner.Combat.PerformAttack(new AttackParams()
                    {
                        Damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target),
                        MaxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target),
                        Target = target,
                        DamageType = DamageType.StandardMelee,
                        Delay = 0
                    });
                }
            }
        }
    }
}