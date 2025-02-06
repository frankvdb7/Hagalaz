using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Bows
{
    /// <summary>
    ///     Contains magic short bow script.
    /// </summary>
    [EquipmentScriptMetaData([861, 13528])]
    public class MagicShortBow : StandardBow
    {
        public MagicShortBow(IProjectileBuilder projectileBuilder) : base(projectileBuilder) { }

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

            // first projectile
            ProjectileBuilder.Create()
                .WithGraphicId(249)
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration(duration)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithDelay(delay)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            // second projectile
            ProjectileBuilder.Create()
                .WithGraphicId(249)
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration((int)(duration * 1.65))
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithDelay(delay)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            var combat = (ICharacterCombat)attacker.Combat;
            var damage1 = combat.GetRangedDamage(victim, true);
            var damage2 = combat.GetRangedDamage(victim, true);
            var maxDamage = combat.GetRangedMaxHit(victim, true);

            // attack 1
            attacker.Combat.PerformAttack(new AttackParams
            {
                Target = victim,
                Damage = damage1,
                MaxDamage = maxDamage,
                DamageType = DamageType.FullRange,
                Delay = duration + delay
            });
            Ammo.RemoveAmmo(attacker, ammo, victim.Location, CreatureHelper.CalculateTicksForClientTicks(duration + delay));

            // attack 2
            attacker.Combat.PerformAttack(new AttackParams
            {
                Target = victim,
                Damage = damage2,
                MaxDamage = maxDamage,
                DamageType = DamageType.FullRange,
                Delay = (duration * 2) + delay
            });
            Ammo.RemoveAmmo(attacker, ammo, victim.Location, CreatureHelper.CalculateTicksForClientTicks(duration * 2 + delay));
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(1074));
                animator.QueueGraphic(Graphic.Create(256, 0, 100));
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
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 550;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            base.OnEquiped(item, character);
            character.AddState(new State(StateType.MagicShortBowEquiped, int.MaxValue));
        }

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character)
        {
            base.OnUnequiped(item, character);
            character.RemoveState(StateType.MagicShortBowEquiped);
        }
    }
}