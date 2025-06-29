using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Npcs.Dragons
{
    /// <summary>
    ///     Contains a metal dragon script.
    /// </summary>
    [EquipmentScriptMetaData([1591, 1592, 3590, 10776, 10777, 10778, 10779, 10780, 10781])]
    public class MetalDragon : StandardDragon
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public MetalDragon(IProjectileBuilder projectileBuilder) => _projectileBuilder = projectileBuilder;

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            Bonus = AttackBonus.Magic;
            Style = AttackStyle.MagicNormal;
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance()
        {
            if (Bonus == AttackBonus.Magic || Owner.Combat.Target != null && !Owner.WithinRange(Owner.Combat.Target, 1))
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
                Bonus = AttackBonus.Magic;
                Style = AttackStyle.MagicNormal;

                const int maxDamage = 550;
                // Dragon fire
                Owner.QueueAnimation(Animation.Create(13160));

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
                    .WithGraphicId(2148)
                    .FromCreature(Owner)
                    .ToCreature(target)
                    .WithDuration(delay)
                    .WithSlope(10)
                    .WithDelay(10)
                    .Send();

                var damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxDamage);
                var handle = Owner.Combat.PerformAttack(new AttackParams()
                {
                    Damage = damage,
                    MaxDamage = maxDamage,
                    DamageType = DamageType.DragonFire,
                    Target = target,
                    Delay = delay
                });

                handle.RegisterResultHandler(_ => { target.QueueGraphic(Graphic.Create(439)); });
            }
            else
            {
                if (RandomStatic.Generator.Next(0, 100) >= 80)
                {
                    Bonus = AttackBonus.Magic;
                    Style = AttackStyle.MagicNormal;

                    const int maxDamage = 550;
                    // Dragon fire
                    Owner.QueueAnimation(Animation.Create(13164));
                    Owner.QueueGraphic(Graphic.Create(2465));

                    var damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxDamage);
                    var handle = Owner.Combat.PerformAttack(new AttackParams()
                    {
                        Damage = damage,
                        MaxDamage = maxDamage,
                        DamageType = DamageType.DragonFire,
                        Target = target,
                        Delay = 2
                    });

                    handle.RegisterResultHandler(_ => { target.QueueGraphic(Graphic.Create(439)); });
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
                        DamageType = DamageType.StandardMelee
                    });
                }
            }
        }
    }
}