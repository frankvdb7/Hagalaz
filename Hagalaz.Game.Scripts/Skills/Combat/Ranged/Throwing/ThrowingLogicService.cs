using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    /// <summary>
    ///     Class for throwing related methods.
    /// </summary>
    public class ThrowingLogicService : IThrowingLogicService
    {
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     Gets the throwing graphic.
        /// </summary>
        /// <param name="ammo">The ammo.</param>
        /// <returns></returns>
        private static int GetThrowingProjectileGraphic(IItem ammo)
        {
            var id = ammo.Id;


            return -1;
        }

        public ThrowingLogicService(IProjectileBuilder projectileBuilder) => _projectileBuilder = projectileBuilder;

        /// <summary>
        ///     Perform's standard throwing attack.
        /// </summary>
        public void PerformThrowingStandardAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var ammo = attacker.Equipment[EquipmentSlot.Weapon];
            if (ammo == null || ammo != item)
            {
                attacker.Combat.CancelTarget();
                return;
            }

            var combat = (ICharacterCombat)attacker.Combat;

            item.EquipmentScript.RenderAttack(item, attacker, false); // might throw NotImplementedException 
            //attacker.QueueGraphic(Graphic.Create(Arrows.GetArrowPullBackGraphic(arrow), 0, 100));

            var delay = Math.Max(10, (int)Location.GetDistance(attacker.Location.X, attacker.Location.Y, victim.Location.X, victim.Location.Y) * 5);

            _projectileBuilder.Create()
                .WithGraphicId(GetThrowingProjectileGraphic(ammo))
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration(delay)
                .WithDelay(41)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            Ammo.RemoveAmmo(attacker, ammo, victim.Location.Clone(), CreatureHelper.CalculateTicksForClientTicks(delay + 41));

            var maxDamage = combat.GetRangedMaxHit(victim, false);
            var damage = combat.GetRangedDamage(victim, false);

            attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                MaxDamage = maxDamage,
                Delay = delay + 41,
                Target = victim,
                DamageType = DamageType.FullRange,
            });
        }
    }
}