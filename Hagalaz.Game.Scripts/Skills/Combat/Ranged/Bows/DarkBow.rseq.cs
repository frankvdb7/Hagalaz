using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Utilities;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Bows
{
    /// <summary>
    ///     Contains dark bow script.
    /// </summary>
    [EquipmentScriptMetaData([11235, 13405, 15701, 15702, 15703, 15704])]
    public class DarkBow : StandardBow
    {
        public DarkBow(IProjectileBuilder projectileBuilder) : base(projectileBuilder) { }

        /// <summary>
        ///     Gets the dark bow pull back.
        /// </summary>
        /// <param name="arrow">The arrow.</param>
        /// <returns></returns>
        private static int GetDarkBowPullBackGraphic(ArrowType arrow)
        {
            switch (arrow)
            {
                case ArrowType.Dragon: return 1111;
                case ArrowType.Rune: return 1109;
                case ArrowType.Adamantine: return 1108;
                case ArrowType.Mithril: return 1107;
                case ArrowType.Steel: return 1106;
                case ArrowType.Iron: return 1105;
                case ArrowType.Bronze: return 1104;
                case ArrowType.FireBronze:
                case ArrowType.FireIron:
                case ArrowType.FireSteel:
                case ArrowType.FireMithril:
                case ArrowType.FireAdamantine:
                case ArrowType.FireRune:
                case ArrowType.FireDragon:
                    return 1113;
                case ArrowType.Ice: return 1110;
                default: return -1;
            }
        }

        /// <summary>
        ///     Performs the standard attack.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="victim">The victim.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var ammo = attacker.Equipment[EquipmentSlot.Arrow];
            if (!Ammo.HasSuitableBowAmmo(attacker, 2) || ammo == null)
            {
                attacker.Combat.CancelTarget();
                return;
            }

            var combat = (ICharacterCombat)attacker.Combat;

            item.EquipmentScript.RenderAttack(item, attacker, false); // might throw NotImplementedException 
            var arrow = Arrows.GetArrowType(attacker);
            attacker.QueueGraphic(Graphic.Create(GetDarkBowPullBackGraphic(arrow), 0, 100)); // draw back graphics


            var duration = Math.Max(10, (int)Location.GetDistance(attacker.Location.X, attacker.Location.Y, victim.Location.X, victim.Location.Y) * 5);
            const int delay = 41;

            // projectile 1
            ProjectileBuilder.Create()
                .WithGraphicId(Arrows.GetArrowGraphic(arrow))
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration(duration)
                .WithDelay(delay)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithAngle(11)
                .WithSlope(5)
                .Send();

            // projectile 2
            ProjectileBuilder.Create()
                .WithGraphicId(Arrows.GetArrowGraphic(arrow))
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration((int)(duration * 1.65))
                .WithDelay(delay)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithAngle(11)
                .WithSlope(5)
                .Send();

            var maxDamage = combat.GetRangedMaxHit(victim, false);
            var damage1 = combat.GetRangedDamage(victim, false);
            var damage2 = combat.GetRangedDamage(victim, false);
            attacker.Combat.PerformAttack(new AttackParams()
            {
                Target = victim,
                DamageType = DamageType.FullRange,
                Damage = damage1,
                MaxDamage = maxDamage,
                Delay = duration + delay
            });
            Ammo.RemoveAmmo(attacker, ammo, victim.Location.Clone(), CreatureHelper.CalculateTicksForClientTicks(duration + delay));

            attacker.Combat.PerformAttack(new AttackParams()
            {
                Target = victim,
                DamageType = DamageType.FullRange,
                Damage = damage2,
                MaxDamage = maxDamage,
                Delay = duration * 2 + delay
            });
            Ammo.RemoveAmmo(attacker, ammo, victim.Location.Clone(), CreatureHelper.CalculateTicksForClientTicks(duration * 2 + delay));
        }

        /// <summary>
        ///     Perform's special attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        public override void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var ammo = attacker.Equipment[EquipmentSlot.Arrow];
            if (!Ammo.HasSuitableBowAmmo(attacker, 2) || ammo == null)
            {
                attacker.Combat.CancelTarget();
                return;
            }

            RenderAttack(item, attacker, true);

            var duration = Math.Max(10, (int)Location.GetDistance(attacker.Location.X, attacker.Location.Y, victim.Location.X, victim.Location.Y) * 5);
            const int delay = 41;

            var arrow = Arrows.GetArrowType(attacker);

            ProjectileBuilder.Create()
                .WithGraphicId(arrow == ArrowType.Dragon ? 1099 : Arrows.GetArrowGraphic(arrow))
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration(duration)
                .WithDelay(delay)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            ProjectileBuilder.Create()
                .WithGraphicId(arrow == ArrowType.Dragon ? 1099 : Arrows.GetArrowGraphic(arrow))
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration((int)(duration * 1.5))
                .WithDelay(delay)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            var combat = (ICharacterCombat)attacker.Combat;
            var damage1 = combat.GetRangedDamage(victim, true);
            var damage2 = combat.GetRangedDamage(victim, true);
            var maxDamage = combat.GetRangedMaxHit(victim, false);
            if (maxDamage > 480)
            {
                maxDamage = 480;
            }

            if (arrow == ArrowType.Dragon)
            {
                if (damage1 < 80)
                {
                    damage1 = 80;
                }

                if (damage2 < 80)
                {
                    damage2 = 80;
                }
            }
            else
            {
                if (damage1 < 50)
                {
                    damage1 = 50;
                }

                if (damage2 < 50)
                {
                    damage2 = 50;
                }
            }

            attacker.Combat.PerformAttack(new AttackParams
            {
                Target = victim,
                Damage = damage1,
                DamageType = DamageType.FullRange,
                Delay = duration + delay,
                MaxDamage = maxDamage
            });
            Ammo.RemoveAmmo(attacker, ammo, victim.Location.Clone(), CreatureHelper.CalculateTicksForClientTicks(duration + delay));

            attacker.Combat.PerformAttack(new AttackParams()
            {
                Target = victim,
                Damage = damage2,
                DamageType = DamageType.FullRange,
                Delay = duration * 2 + delay,
                MaxDamage = maxDamage
            });
            Ammo.RemoveAmmo(attacker, ammo, victim.Location.Clone(), CreatureHelper.CalculateTicksForClientTicks(duration * 2 + delay));
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(426));
                animator.QueueGraphic(Graphic.Create(GetDarkBowPullBackGraphic(Arrows.GetArrowType(animator)), 0, 100));
            }
            else
            {
                base.RenderAttack(item, animator, specialAttack);
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <returns></returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 650;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            base.OnEquiped(item, character);
            character.AddState(new DarkBowEquippedState());
        }

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character)
        {
            base.OnUnequiped(item, character);
            character.RemoveState<DarkBowEquippedState>();
        }
    }
}