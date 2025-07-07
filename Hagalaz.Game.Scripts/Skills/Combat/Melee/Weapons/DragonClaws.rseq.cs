using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains dragon claws equipment script.
    /// </summary>
    [EquipmentScriptMetaData([14484, 14486])]
    public class DragonClaws : EquipmentScript
    {
        private readonly IHitSplatBuilder _hitSplatBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DragonClaws"/> class.
        /// </summary>
        /// <param name="hitSplatBuilder">The hit splat builder.</param>
        public DragonClaws(IHitSplatBuilder hitSplatBuilder) => _hitSplatBuilder = hitSplatBuilder;

        /// <summary>
        ///     Perform's special attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        public override void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var combat = (ICharacterCombat)attacker.Combat;
            attacker.QueueAnimation(Animation.Create(10961));
            attacker.QueueGraphic(Graphic.Create(1950));

            var standardMax = combat.GetMeleeMaxHit(victim, false);
            var fullMax = combat.GetMeleeMaxHit(victim, true);

            var damage1 = combat.GetMeleeDamage(victim, true);
            var damage2 = -1;
            var damage3 = -1;
            var damage4 = -1;
            if (damage1 != -1)
            {
                damage2 = (int)(damage1 * 0.50);
                damage3 = damage2 / 2;
                damage4 = damage3 + 1;
            }
            else if ((damage2 = combat.GetMeleeDamage(victim, true)) != -1)
            {
                damage3 = (int)(damage2 * 0.50);
                damage4 = (int)(damage2 * 0.50) + 1;
            }
            else if ((damage3 = combat.GetMeleeDamage(victim, true)) != -1)
            {
                damage4 = damage3;
                var max = (int)(fullMax * 0.75);
                if (damage3 > max)
                {
                    damage3 = max;
                }

                if (damage4 > max)
                {
                    damage4 = max;
                }

                damage4++;
            }
            else if ((damage4 = combat.GetMeleeDamage(victim, true)) != -1)
            {
                damage4 = (int)(damage4 * 1.5);
            }
            else
            {
                damage4 = RandomStatic.Generator.Next(0, 8);
            }

            combat.PerformSoulSplit(victim,
                CreatureHelper.CalculatePredictedDamage([
                    damage1, damage2, damage3, damage4
                ]));

            combat.AddMeleeExperience(damage1);
            combat.AddMeleeExperience(damage2);
            combat.AddMeleeExperience(damage3);
            combat.AddMeleeExperience(damage4);

            if (damage1 == -1 && damage2 == -1 && damage3 == -1 && damage4 < 8)
            {
                damage1 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage1, 0);
                damage4 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage4, 20);

                var soak1 = -1;
                damage1 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage1, ref soak1);

                var splat1Builder = _hitSplatBuilder.Create()
                    .AddSprite(sprite => sprite
                        .WithDamage(damage1)
                        .WithSplatType(HitSplatType.HitMiss))
                    .FromSender(attacker);

                if (soak1 != -1)
                {
                    splat1Builder.AddSprite(sprite => sprite
                        .WithSplatType(HitSplatType.HitDefendedDamage)
                        .WithDamage(soak1));
                }

                victim.QueueHitSplat(splat1Builder.Build());
                victim.QueueTask(new RsTask(() =>
                    {
                        var soak4 = -1;
                        damage4 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage4, ref soak4);

                        var splat2Builder = _hitSplatBuilder.Create()
                            .AddSprite(sprite => sprite
                                .WithSplatType(HitSplatType.HitMeleeDamage)
                                .WithDamage(damage4)
                                .WithMaxDamage(standardMax))
                            .FromSender(attacker);
                        ;

                        if (soak4 != -1)
                        {
                            splat2Builder.AddSprite(sprite => sprite
                                .WithSplatType(HitSplatType.HitDefendedDamage)
                                .WithDamage(soak4));
                        }

                        victim.QueueHitSplat(splat2Builder.Build());
                    },
                    1));
            }
            else
            {
                damage1 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage1, 0);
                damage2 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage2, 0);
                damage3 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage3, 20);
                damage4 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage4, 20);

                var soak1 = -1;
                var soak2 = -1;
                damage1 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage1, ref soak1);
                damage2 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage2, ref soak2);

                var splat1Builder = _hitSplatBuilder.Create()
                    .AddSprite(sprite => sprite
                        .WithSplatType(HitSplatType.HitMeleeDamage)
                        .WithDamage(damage1)
                        .WithMaxDamage(standardMax))
                    .FromSender(attacker);

                var splat2Builder = _hitSplatBuilder.Create()
                    .AddSprite(sprite => sprite
                        .WithSplatType(HitSplatType.HitMeleeDamage)
                        .WithDamage(damage2)
                        .WithMaxDamage(standardMax))
                    .FromSender(attacker);

                if (soak1 != -1)
                {
                    splat1Builder.AddSprite(sprite => sprite
                        .WithSplatType(HitSplatType.HitDefendedDamage)
                        .WithDamage(soak1));
                }

                if (soak2 != -1)
                {
                    splat2Builder.AddSprite(sprite => sprite
                        .WithSplatType(HitSplatType.HitDefendedDamage)
                        .WithDamage(soak2));
                }

                victim.QueueHitSplat(splat1Builder.Build());
                victim.QueueHitSplat(splat2Builder.Build());
                victim.QueueTask(new RsTask(() =>
                    {
                        var soak3 = -1;
                        var soak4 = -1;
                        damage3 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage3, ref soak3);
                        damage4 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage4, ref soak4);

                        var splat3Builder = _hitSplatBuilder.Create()
                            .AddSprite(sprite => sprite
                                .WithSplatType(HitSplatType.HitMeleeDamage)
                                .WithDamage(damage3)
                                .WithMaxDamage(standardMax))
                            .FromSender(attacker);

                        var splat4Builder = _hitSplatBuilder.Create()
                            .AddSprite(sprite => sprite
                                .WithSplatType(HitSplatType.HitMeleeDamage)
                                .WithDamage(damage4)
                                .WithMaxDamage(standardMax))
                            .FromSender(attacker);

                        if (soak3 != -1)
                        {
                            splat3Builder.AddSprite(sprite => sprite
                                .WithSplatType(HitSplatType.HitDefendedDamage)
                                .WithDamage(soak3));
                        }

                        if (soak4 != -1)
                        {
                            splat4Builder.AddSprite(sprite => sprite
                                .WithSplatType(HitSplatType.HitDefendedDamage)
                                .WithDamage(soak4));
                        }

                        victim.QueueHitSplat(splat3Builder.Build());
                        victim.QueueHitSplat(splat4Builder.Build());
                    },
                    1));
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 500;
    }
}