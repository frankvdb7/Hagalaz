using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Bows
{
    /// <summary>
    ///     Contains standard bow script.
    /// </summary>
    [EquipmentScriptMetaData([
        839, 841, 843, 845, 847, 849, 851, 13523, 853, 13524, 855, 13525, 857, 13526, 859, 13527, 4212, 4214, 4215, 4216, 4217, 4218, 4219, 4220, 4221,
        4222, 4223, 4827, 9705, 10280, 13541, 10282, 13542, 10284, 13543, 14121, 16317, 16319, 16321, 16323, 16325, 16327, 16329, 16331, 16333, 16335,
        16337, 16867, 16869, 16871, 16873, 16875, 16877, 16879, 16881, 16883, 16885, 16887, 16975, 17295, 21332, 18331, 18332, 19143, 19145, 19146, 19148,
        19149, 19151, 20171, 20173
    ])]
    public class StandardBow : EquipmentScript
    {
        protected readonly IProjectileBuilder ProjectileBuilder;

        public StandardBow(IProjectileBuilder projectileBuilder) => ProjectileBuilder = projectileBuilder;

        /// <summary>
        ///     Performs the standard attack.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="victim">The victim.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var arrow = Arrows.GetArrowType(attacker);
            if (arrow != ArrowType.CrystalBowInfinite && arrow != ArrowType.ZaryteBowInfinite && (!Ammo.HasSuitableBowAmmo(attacker)))
            {
                attacker.Combat.CancelTarget();
                return;
            }

            var combat = (ICharacterCombat)attacker.Combat;

            item.EquipmentScript.RenderAttack(item, attacker, false); // might throw NotImplementedException
            attacker.QueueGraphic(Graphic.Create(Arrows.GetArrowPullBackGraphic(arrow), 0, 100));

            var duration = Math.Max(10, (int)Location.GetDistance(attacker.Location.X, attacker.Location.Y, victim.Location.X, victim.Location.Y) * 5);
            const int delay = 41;

            ProjectileBuilder.Create()
                .WithGraphicId(Arrows.GetArrowGraphic(arrow))
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration(duration)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithDelay(delay)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            if (arrow != ArrowType.ZaryteBowInfinite && arrow != ArrowType.CrystalBowInfinite)
            {
                var ammo = attacker.Equipment[EquipmentSlot.Arrow];
                Ammo.RemoveAmmo(attacker, ammo!, victim.Location, CreatureHelper.CalculateTicksForClientTicks(duration + delay));
            }

            var maxDamage = combat.GetRangedMaxHit(victim, false);
            var damage = combat.GetRangedDamage(victim, false);
            attacker.Combat.PerformAttack(new AttackParams
            {
                Target = victim,
                DamageType = DamageType.FullRange,
                Damage = damage,
                MaxDamage = maxDamage,
                Delay = duration + delay
            });
        }

        /// <summary>
        ///     Happens when this item is equiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new BowEquipedState());

        /// <summary>
        ///     Happens when this item is unequiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<BowEquipedState>();
    }
}